using Tsubakimoto.SpeechTranslator.WinUIApp.Core;

namespace Tsubakimoto.SpeechTranslator.WinUIApp.Tests;

public sealed partial class MainViewModelTests
{
    /// <summary>Results are accepted during pending and running start, but ignored before start and after terminal boundaries.</summary>
    [Theory]
    [InlineData("before")] [InlineData("pending-start")] [InlineData("running")] [InlineData("failed-start")] [InlineData("ended")] [InlineData("failed-stop")] [InlineData("stopped")]
    public async Task Translated_OnlyWhileAccepting_AddsOrderedResultsAndActivityAsync(string state)
    {
        // Arrange
        using var h = new MainViewModelTestHarness(Valid); await h.InitializeAsync(); var startGate = new TaskCompletionSource();
        if (state is "pending-start" or "failed-start") h.Speech.StartAsync(Arg.Any<Settings>()).Returns(startGate.Task);
        Task start = Task.CompletedTask;
        if (state != "before") start = h.Model.StartAsync();
        if (state == "failed-start") { startGate.SetException(new IOException()); h.Pump(); }
        if (state == "ended") { h.End(); h.Pump(); }
        if (state is "failed-stop" or "stopped") { if (state == "failed-stop") h.Speech.StopAsync().Returns(Task.FromException(new IOException())); var stop = h.Model.StopAsync(); h.Pump(); MainViewModelTestHarness.Completed(stop); await stop; }
        var activity = h.Model.Activity.ToArray();
        // Act
        h.Translate("first", "一"); h.Translate("second", "二");
        // Assert
        if (state is "pending-start" or "running")
        {
            Assert.Equal(new[] { new Translation("first", "一"), new Translation("second", "二") }, h.Model.Translations);
            Assert.Equal(new[] { "原文: first", "翻訳: 一", "原文: second", "翻訳: 二" }, MainViewModelTestHarness.Messages(h.Model).TakeLast(4));
        }
        else { Assert.Empty(h.Model.Translations); Assert.Equal(activity, h.Model.Activity); }
        h.Recording.DidNotReceive().Append(Arg.Any<string>(), Arg.Any<Translation>());
        if (state == "pending-start") { startGate.SetResult(); h.Pump(); } h.Pump(); MainViewModelTestHarness.Completed(start); await start;
    }

    /// <summary>The stop-request boundary rejects a synchronous result before the StopCore yield is pumped.</summary>
    [Fact]
    public async Task StopAsync_ImmediateTranslationBeforeYield_IsIgnoredAsync()
    {
        // Arrange
        using var h = new MainViewModelTestHarness(Valid); await h.InitializeAsync(); h.Recording.Validate(null, "").Returns("record.txt"); await h.Model.StartAsync(); var before = h.Model.Activity.ToArray();
        // Act
        var stop = h.Model.StopAsync(); h.Translate("late", "遅延");
        var results = h.Model.Translations.ToArray(); var activity = h.Model.Activity.ToArray(); var appends = h.Recording.ReceivedCalls().Count(c => c.GetMethodInfo().Name == "Append");
        h.Pump(); MainViewModelTestHarness.Completed(stop); await stop;
        // Assert
        Assert.Empty(results); Assert.Equal(before, activity); Assert.Equal(0, appends);
    }

    /// <summary>Each speech event dispatches collection mutation; idle Ended logs without stopping.</summary>
    [Fact]
    public async Task SpeechEvents_DispatchBeforeCollectionMutationAsync()
    {
        // Arrange
        using var h = new MainViewModelTestHarness(Valid); await h.InitializeAsync(); h.QueueDispatch = true;
        // Act
        h.Status("queued-status"); h.End("idle-ended");
        // Assert
        Assert.Empty(h.Model.Activity); h.DrainDispatcher(); Assert.Equal(new[] { "queued-status", "idle-ended" }, MainViewModelTestHarness.Messages(h.Model)); await h.Speech.DidNotReceive().StopAsync();
        await h.Model.StartAsync(); var prefix = h.Model.Activity.ToArray(); h.Translate("queued-source", "queued-target"); Assert.Empty(h.Model.Translations); Assert.Equal(prefix, h.Model.Activity); h.DrainDispatcher(); Assert.Equal(new Translation("queued-source", "queued-target"), Assert.Single(h.Model.Translations)); Assert.Equal(new[] { "原文: queued-source", "翻訳: queued-target" }, MainViewModelTestHarness.Messages(h.Model).TakeLast(2));
        var gate = new TaskCompletionSource(); h.Speech.StopAsync().Returns(gate.Task); prefix = h.Model.Activity.ToArray(); h.End("frozen-ended"); Assert.Equal(prefix, h.Model.Activity); await h.Speech.DidNotReceive().StopAsync(); h.DrainDispatcher(); await h.Speech.Received(1).StopAsync(); h.Translate("after-ended", "ignored"); h.DrainDispatcher(); Assert.Single(h.Model.Translations);
        var stop = h.Model.StopAsync(); gate.SetResult(); h.Pump(); h.DrainDispatcher(); MainViewModelTestHarness.Completed(stop); await stop; Assert.False(h.Model.IsFrozen);
    }

    /// <summary>Recording failures occur after display, stop once and never replay or append later results.</summary>
    [Theory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public async Task Translated_RecordingAppendFailure_StopsOnceWithoutReplayingResultAsync(bool otherException, bool failedStop)
    {
        // Arrange
        using var h = new MainViewModelTestHarness(Valid); await h.InitializeAsync(); h.Model.Filename = "record"; h.Recording.Validate(null, "record").Returns("validated.txt"); var gate = new TaskCompletionSource(); h.Speech.StopAsync().Returns(gate.Task);
        h.Recording.When(r => r.Append(Arg.Any<string>(), Arg.Any<Translation>())).Do(_ =>
        {
            Assert.Equal(new Translation("source", "訳"), Assert.Single(h.Model.Translations)); Assert.Equal(new[] { "原文: source", "翻訳: 訳" }, MainViewModelTestHarness.Messages(h.Model).TakeLast(2));
            throw otherException ? new InvalidOperationException("recording") : new IOException("recording");
        }); await h.Model.StartAsync();
        // Act
        h.Translate(); h.Translate("ignored", "ignored"); h.Pump(); var stop = h.Model.StopAsync();
        // Assert
        h.Recording.Received(1).Append("validated.txt", new Translation("source", "訳")); Assert.Single(h.Model.Translations); Assert.Contains(MainViewModelTestHarness.Messages(h.Model), s => s.Contains("書き込みに失敗")); await h.Speech.Received(1).StopAsync(); Assert.True(h.Model.IsFrozen); Assert.False(h.Model.CanEdit || h.Model.CanStart);
        if (failedStop) gate.SetException(new IOException()); else gate.SetResult(); h.Pump(); MainViewModelTestHarness.Completed(stop); await stop; h.Translate("later", "ignored"); Assert.Single(h.Model.Translations); h.Recording.Received(1).Append(Arg.Any<string>(), Arg.Any<Translation>()); Assert.Equal(failedStop, h.Model.IsFrozen); Assert.Equal(failedStop, h.Model.HasStopError);
        if (failedStop) { Assert.True(h.Model.CanStop); h.Speech.StopAsync().Returns(Task.CompletedTask); var retry = h.Model.StopAsync(); h.Pump(); MainViewModelTestHarness.Completed(retry); await retry; Assert.False(h.Model.IsFrozen); }
    }

    /// <summary>Sessions retain empty, singleton and interior ordered histories and capture fresh recording paths only at start.</summary>
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(64)]
    public async Task MultipleSessions_RetainAllLogsAndTranslationsAsync(int count)
    {
        // Arrange
        using var h = new MainViewModelTestHarness(Valid); await h.InitializeAsync(); h.Model.Filename = "first"; h.Recording.Validate(null, "first").Returns("first.txt"); h.Recording.Validate(null, "second").Returns("second.txt"); await h.Model.StartAsync(); var expected = new List<Translation>();
        // Act
        for (var i = 0; i < count; i++) { expected.Add(new($"source-{i}", $"訳-{i}")); h.Translate($"source-{i}", $"訳-{i}"); }
        h.Model.Filename = "second"; var prefix = h.Model.Activity.ToArray(); var stop = h.Model.StopAsync(); h.Pump(); MainViewModelTestHarness.Completed(stop); await stop;
        await h.Model.StartAsync(); h.Translate("new-session", "新規"); expected.Add(new("new-session", "新規"));
        // Assert
        Assert.Equal(expected, h.Model.Translations); Assert.Equal(prefix, h.Model.Activity.Take(prefix.Length));
        h.Recording.Received(1).Validate(null, "first"); h.Recording.Received(1).Validate(null, "second"); h.Recording.Received(count).Append("first.txt", Arg.Any<Translation>()); h.Recording.Received(1).Append("second.txt", new Translation("new-session", "新規"));
        if (count > 0) { Assert.Equal(new Translation("source-0", "訳-0"), h.Model.Translations[0]); Assert.Equal(new Translation($"source-{count / 2}", $"訳-{count / 2}"), h.Model.Translations[count / 2]); }
        stop = h.Model.StopAsync(); h.Pump(); MainViewModelTestHarness.Completed(stop); await stop; h.Translate("after-stop", "ignored"); Assert.Equal(expected, h.Model.Translations);
        h.Recording.Validate(null, "second").Returns((string?)null); await h.Model.StartAsync(); h.Translate("no-record", "記録なし"); h.Recording.Received(count + 1).Append(Arg.Any<string>(), Arg.Any<Translation>());
    }

    /// <summary>Changing filename during recognition does not redirect the captured recording destination.</summary>
    [Fact]
    public async Task Translated_FilenameEditedDuringSession_UsesCapturedDestinationAsync()
    {
        // Arrange
        using var h = new MainViewModelTestHarness(Valid); await h.InitializeAsync(); h.Model.Filename = "first";
        h.Recording.Validate(null, "first").Returns("first.txt"); h.Recording.Validate(null, "second").Returns("second.txt"); await h.Model.StartAsync();
        // Act
        h.Model.Filename = "second"; h.Translate("active", "稼働中");
        // Assert
        h.Recording.Received(1).Append("first.txt", new Translation("active", "稼働中")); h.Recording.DidNotReceive().Validate(null, "second");
        var stop = h.Model.StopAsync(); h.Pump(); MainViewModelTestHarness.Completed(stop); await stop;
        await h.Model.StartAsync(); h.Translate("next", "次回"); h.Recording.Received(1).Append("second.txt", new Translation("next", "次回"));
    }

    /// <summary>A result queued during acceptance is ignored if stop is requested before dispatcher execution.</summary>
    [Fact]
    public async Task LateQueuedTranslation_AfterStop_IsIgnoredAsync()
    {
        // Arrange
        using var h = new MainViewModelTestHarness(Valid); await h.InitializeAsync(); h.Recording.Validate(null, "").Returns("record.txt"); await h.Model.StartAsync(); h.QueueDispatch = true; var before = h.Model.Activity.ToArray(); h.Translate("queued", "遅延"); Assert.Empty(h.Model.Translations);
        // Act
        var stop = h.Model.StopAsync(); h.DrainDispatcher();
        // Assert
        var results = h.Model.Translations.ToArray(); var activity = h.Model.Activity.ToArray(); h.Pump(); h.DrainDispatcher(); MainViewModelTestHarness.Completed(stop); await stop;
        Assert.Empty(results); Assert.DoesNotContain(activity.Skip(before.Length), s => s.Contains("原文:") || s.Contains("翻訳:")); h.Recording.DidNotReceive().Append(Arg.Any<string>(), Arg.Any<Translation>());
    }
}
