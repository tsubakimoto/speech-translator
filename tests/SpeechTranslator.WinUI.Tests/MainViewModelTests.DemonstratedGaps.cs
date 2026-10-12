using SpeechTranslatorWinUI.Core;

namespace SpeechTranslator.WinUI.Tests;

public sealed partial class MainViewModelTests
{
    /// <summary>Invalid committed credentials and initialization locks deny start even with valid unsaved drafts.</summary>
    [Theory]
    [InlineData("empty", "", "")]
    [InlineData("region-only", "japaneast", "")]
    [InlineData("key-only", "", "saved-key")]
    [InlineData("invalid-region", "INVALID", "saved-key")]
    [InlineData("invalid-key", "japaneast", "bad key")]
    [InlineData("uninitialized", "japaneast", "saved-key")]
    [InlineData("recovery", "japaneast", "saved-key")]
    public async Task StartAsync_InvalidCommittedCredentials_DoesNotValidateFreezeOrStartAsync(string state, string region, string key)
    {
        // Arrange
        var saved = new Settings(region, key, "ja-JP", "en-US", "saved-folder");
        using var h = new MainViewModelTestHarness(saved);
        if (state == "recovery") h.Store.LoadAsync().Returns(Task.FromException<Settings?>(new IOException("controlled load failure")));
        if (state != "uninitialized") await h.InitializeAsync();
        var expected = state is "uninitialized" or "recovery" ? new Settings() : saved;
        h.Model.Region = "westus"; h.Model.Key = "draft-key"; h.Model.Filename = "chosen-record";
        var activity = h.Model.Activity.ToArray(); var translations = h.Model.Translations.ToArray();
        var status = h.Model.Status; var message = h.Model.SettingsMessage;
        var frozenChanges = new List<bool>();
        h.Model.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(h.Model.IsFrozen)) frozenChanges.Add(h.Model.IsFrozen); };

        // Act
        var start = h.Model.StartAsync();
        MainViewModelTestHarness.Completed(start); await start; h.Pump();

        // Assert
        h.Recording.DidNotReceive().Validate(Arg.Any<string?>(), Arg.Any<string>());
        h.Recording.DidNotReceive().Append(Arg.Any<string>(), Arg.Any<Translation>());
        await h.Speech.DidNotReceive().StartAsync(Arg.Any<Settings>()); await h.Speech.DidNotReceive().StopAsync();
        await h.Store.DidNotReceive().SaveAsync(Arg.Any<Settings>()); await h.Store.DidNotReceive().RecoverAsync(Arg.Any<Settings>());
        Assert.Equal(expected, h.Model.CommittedSettings);
        Assert.Equal(("westus", "draft-key", "chosen-record"), (h.Model.Region, h.Model.Key, h.Model.Filename));
        Assert.Equal((expected.Source, expected.Target, expected.Folder ?? ""), (h.Model.Source, h.Model.Target, h.Model.Folder));
        Assert.Equal(activity, h.Model.Activity); Assert.Equal(translations, h.Model.Translations);
        Assert.Equal(status, h.Model.Status); Assert.Equal(message, h.Model.SettingsMessage); Assert.Empty(frozenChanges);
        Assert.False(h.Model.IsFrozen || h.Model.IsStopPending || h.Model.HasStopError || h.Model.IsPersisting || h.Model.CanStart || h.Model.CanStop);
        Assert.Equal(state != "uninitialized", h.Model.IsInitialized); Assert.Equal(state == "recovery", h.Model.HasRecoveryError);
        Assert.Equal(state is not ("uninitialized" or "recovery"), h.Model.CanEdit); Assert.True(h.Model.HasUnsavedChanges);
    }

    /// <summary>Each nonpersisting edit lock restores attempted preferences without writing or discarding drafts.</summary>
    [Theory]
    [InlineData("uninitialized")]
    [InlineData("frozen")]
    [InlineData("recovery")]
    public async Task SavePreferencesAsync_UninitializedFrozenOrRecoveryRequired_RestoresDisplayWithoutWritingAsync(string state)
    {
        // Arrange
        var saved = Valid with { Folder = "saved-folder" };
        using var h = new MainViewModelTestHarness(saved);
        if (state == "recovery") h.Store.LoadAsync().Returns(Task.FromException<Settings?>(new IOException("controlled load failure")));
        if (state != "uninitialized") await h.InitializeAsync();
        if (state == "frozen") await h.Model.StartAsync();
        var expected = state == "frozen" ? saved : new Settings();
        h.Model.Region = " raw-westus "; h.Model.Key = " raw-draft-key "; h.Model.Filename = "chosen-record";
        h.Model.Source = "attempted-source"; h.Model.Target = "attempted-target"; h.Model.Folder = "attempted-folder";
        var activity = h.Model.Activity.ToArray(); var status = h.Model.Status; var message = h.Model.SettingsMessage;

        // Act
        var result = await h.Model.SavePreferencesAsync("ja-JP", "en-US", null);

        // Assert
        Assert.False(result); await h.Store.DidNotReceive().SaveAsync(Arg.Any<Settings>());
        await h.Store.DidNotReceive().RecoverAsync(Arg.Any<Settings>());
        Assert.Equal(expected, h.Model.CommittedSettings);
        Assert.Equal(("en-US", "ja-JP", expected.Folder ?? ""), (h.Model.Source, h.Model.Target, h.Model.Folder));
        Assert.Equal((" raw-westus ", " raw-draft-key ", "chosen-record"), (h.Model.Region, h.Model.Key, h.Model.Filename));
        Assert.True(h.Model.HasUnsavedChanges); Assert.False(h.Model.IsPersisting || h.Model.CanEdit || h.Model.CanStart || h.Model.IsStopPending || h.Model.HasStopError);
        Assert.Equal(state == "frozen", h.Model.IsFrozen); Assert.Equal(state == "recovery", h.Model.HasRecoveryError);
        Assert.Equal(state != "uninitialized", h.Model.IsInitialized);
        Assert.Equal(activity, h.Model.Activity); Assert.Empty(h.Model.Translations);
        Assert.Equal(status, h.Model.Status); Assert.Equal(message, h.Model.SettingsMessage);
        if (state == "frozen") await h.Speech.Received(1).StartAsync(saved); else await h.Speech.DidNotReceive().StartAsync(Arg.Any<Settings>());
        await h.Speech.DidNotReceive().StopAsync();
    }

    /// <summary>Captured display snapshots and automatic stop are asserted outside the caught callback, before caller stop can mask them.</summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Translated_RecordingAppendFailure_ObservesDisplayAndAutomaticStopBeforeCallerStopAsync(bool otherException, bool failedStop)
    {
        // Arrange
        using var h = new MainViewModelTestHarness(Valid); await h.InitializeAsync();
        h.Model.Filename = "record"; h.Recording.Validate(null, "record").Returns("validated.txt");
        var gate = new TaskCompletionSource(); h.Speech.StopAsync().Returns(gate.Task);
        Translation[]? observedTranslations = null; string[]? observedMessages = null;
        var recordingError = otherException ? (Exception)new InvalidOperationException("recording") : new IOException("recording");
        h.Recording.When(r => r.Append(Arg.Any<string>(), Arg.Any<Translation>())).Do(_ =>
        {
            observedTranslations = h.Model.Translations.ToArray();
            observedMessages = MainViewModelTestHarness.Messages(h.Model).TakeLast(2).ToArray();
            throw recordingError;
        });
        await h.Model.StartAsync();

        // Act
        h.Translate(); h.Translate("ignored", "ignored"); h.Pump();

        // Assert: no caller StopAsync has run at this observation boundary.
        Assert.NotNull(observedTranslations); Assert.Equal(new[] { new Translation("source", "訳") }, observedTranslations);
        Assert.NotNull(observedMessages); Assert.Equal(new[] { "原文: source", "翻訳: 訳" }, observedMessages);
        await h.Speech.Received(1).StopAsync(); Assert.True(h.Model.IsStopPending); Assert.True(h.Model.IsFrozen);
        Assert.False(h.Model.CanEdit || h.Model.CanStart || h.Model.CanStop);
        h.Recording.Received(1).Append("validated.txt", new Translation("source", "訳"));
        Assert.Equal(new[] { new Translation("source", "訳") }, h.Model.Translations);
        Assert.Contains(MainViewModelTestHarness.Messages(h.Model), s => s.Contains("書き込みに失敗"));
        var stop = h.Model.StopAsync(); Assert.False(stop.IsCompleted); Assert.Same(stop, h.Model.StopAsync());
        await h.Speech.Received(1).StopAsync();
        if (failedStop) gate.SetException(new IOException("controlled stop failure")); else gate.SetResult();
        h.Pump(); MainViewModelTestHarness.Completed(stop); await stop; h.Translate("later", "ignored");
        Assert.Equal(new[] { new Translation("source", "訳") }, h.Model.Translations);
        h.Recording.Received(1).Append(Arg.Any<string>(), Arg.Any<Translation>()); await h.Speech.Received(1).StopAsync();
        Assert.Equal(failedStop, h.Model.IsFrozen); Assert.Equal(failedStop, h.Model.HasStopError); Assert.False(h.Model.IsStopPending);
        if (failedStop)
        {
            Assert.True(h.Model.CanStop); h.Speech.StopAsync().Returns(Task.CompletedTask);
            var retry = h.Model.StopAsync(); Assert.NotSame(stop, retry); h.Pump(); MainViewModelTestHarness.Completed(retry); await retry;
            await h.Speech.Received(2).StopAsync(); Assert.False(h.Model.IsFrozen || h.Model.HasStopError);
            h.Translate("after-retry", "ignored"); Assert.Single(h.Model.Translations); h.Recording.Received(1).Append(Arg.Any<string>(), Arg.Any<Translation>());
        }
    }
}
