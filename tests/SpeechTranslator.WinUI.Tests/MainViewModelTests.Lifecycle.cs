using NSubstitute.Extensions;
using SpeechTranslatorWinUI.Core;

namespace SpeechTranslator.WinUI.Tests;

public sealed partial class MainViewModelTests
{
    /// <summary>Start freezes immediately, captures committed settings and prevents duplicate validation and speech starts.</summary>
    [Fact]
    public async Task StartAsync_ValidCommittedSettings_FreezesBeforePendingStartAndRejectsDuplicateAsync()
    {
        // Arrange
        var old = Valid with { Folder = "committed-folder" }; using var h = new MainViewModelTestHarness(old); await h.InitializeAsync(); h.Model.Region = "bad draft"; h.Model.Key = "bad key"; h.Model.Filename = "selected";
        var startGate = new TaskCompletionSource(); h.Speech.StartAsync(Arg.Any<Settings>()).Returns(startGate.Task); h.Recording.Validate(old.Folder, "selected").Returns("selected.txt");
        // Act
        var start = h.Model.StartAsync(); await h.Model.StartAsync();
        // Assert
        Assert.True(h.Model.IsFrozen); Assert.False(h.Model.CanStart || h.Model.CanEdit || h.Model.HasStopError); Assert.False(start.IsCompleted);
        h.Recording.Received(1).Validate(old.Folder, "selected"); await h.Speech.Received(1).StartAsync(old); Assert.Equal("開始中…", h.Model.Status);
        startGate.SetResult(); h.Pump(); MainViewModelTestHarness.Completed(start); await start; Assert.Equal("翻訳中", h.Model.Status);
    }

    /// <summary>Validation errors explain the destination problem and never freeze or start speech.</summary>
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task StartAsync_InvalidDestination_DoesNotStartOrFreezeAsync(bool argument)
    {
        // Arrange
        using var h = new MainViewModelTestHarness(Valid); await h.InitializeAsync(); h.Model.Filename = "invalid"; h.Recording.Validate(null, "invalid").Returns(_ => throw (argument ? new ArgumentException("invalid stem") : new IOException("missing folder")));
        // Act
        await h.Model.StartAsync();
        // Assert
        Assert.False(h.Model.IsFrozen); Assert.True(h.Model.CanEdit && h.Model.CanStart); await h.Speech.DidNotReceive().StartAsync(Arg.Any<Settings>()); Assert.Contains("記録先", h.Model.Status); Assert.Contains(argument ? "invalid stem" : "missing folder", h.Model.Status); Assert.Single(h.Model.Activity);
    }

    /// <summary>A previously selected folder deleted before start cannot silently fall back to another destination.</summary>
    [Fact]
    public async Task StartAsync_DeletedSelectedFolder_DoesNotStartAsync()
    {
        // Arrange
        using var directory = new TestDirectory(); var folder = directory.GetPath("selected"); Directory.CreateDirectory(folder); using var h = new MainViewModelTestHarness(Valid, new RecordingWriter()); await h.InitializeAsync(); Assert.True(await h.Model.SavePreferencesAsync("en-US", "ja-JP", folder)); Directory.Delete(folder); h.Model.Filename = "transcript";
        // Act
        await h.Model.StartAsync();
        // Assert
        Assert.False(h.Model.IsFrozen); Assert.Equal(folder, h.Model.CommittedSettings.Folder); Assert.Contains("記録先", h.Model.Status); await h.Speech.DidNotReceive().StartAsync(Arg.Any<Settings>()); Assert.Empty(directory.Snapshot());
    }

    /// <summary>A blank filename permits no folder and displays results without appending.</summary>
    [Theory]
    [InlineData("")] [InlineData(" \t\r\n")]
    public async Task StartAsync_BlankFilename_AllowsNoFolderAndDoesNotAppendAsync(string filename)
    {
        // Arrange
        using var h = new MainViewModelTestHarness(Valid); await h.InitializeAsync(); h.Model.Filename = filename; h.Recording.Validate(null, filename).Returns((string?)null);
        // Act
        await h.Model.StartAsync(); h.Translate("one", "一"); h.Translate("two", "二");
        // Assert
        await h.Speech.Received(1).StartAsync(Valid); h.Recording.Received(1).Validate(null, filename); h.Recording.DidNotReceive().Append(Arg.Any<string>(), Arg.Any<Translation>());
        Assert.Equal(new[] { new Translation("one", "一"), new Translation("two", "二") }, h.Model.Translations); Assert.Equal(new[] { "原文: one", "翻訳: 一", "原文: two", "翻訳: 二" }, MainViewModelTestHarness.Messages(h.Model).TakeLast(4));
    }

    /// <summary>Both synchronous and asynchronous failed starts automatically clean up and remain frozen until cleanup completes.</summary>
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task StartAsync_Failure_InitiatesCleanupAndUnfreezesOnlyAfterStopSuccessAsync(bool asynchronous)
    {
        // Arrange
        using var h = new MainViewModelTestHarness(Valid); await h.InitializeAsync(); var startGate = new TaskCompletionSource(); var stopGate = new TaskCompletionSource();
        h.Speech.StartAsync(Arg.Any<Settings>()).Returns(_ => asynchronous ? startGate.Task : throw new IOException("start failed")); h.Speech.StopAsync().Returns(stopGate.Task);
        // Act
        var start = h.Model.StartAsync(); if (asynchronous) { startGate.SetException(new IOException("start failed")); } h.Pump();
        // Assert
        Assert.Equal(1, h.Speech.ReceivedCalls().Count(c => c.GetMethodInfo().Name == "StopAsync")); // Required automatic cleanup; do not manually initiate it to hide a defect.
        Assert.True(h.Model.IsFrozen); Assert.False(h.Model.CanEdit || h.Model.CanStart); h.Translate(); Assert.Empty(h.Model.Translations); Assert.DoesNotContain("翻訳中", MainViewModelTestHarness.Messages(h.Model));
        var stop = h.Model.StopAsync(); stopGate.SetResult(); h.Pump(); MainViewModelTestHarness.Completed(start); MainViewModelTestHarness.Completed(stop); await start; await stop;
        Assert.False(h.Model.IsFrozen || h.Model.HasStopError || h.Model.IsStopPending); Assert.True(h.Model.CanStart);
    }

    /// <summary>Failed automatic cleanup leaves editing locked and permits a fresh successful manual retry.</summary>
    [Theory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public async Task StartAsync_Failure_CleanupFailureLeavesFrozenAndAllowsRetryAsync(bool asynchronousStart, bool asynchronousStop)
    {
        // Arrange
        using var h = new MainViewModelTestHarness(Valid); await h.InitializeAsync(); var startGate = new TaskCompletionSource(); var cleanup = new TaskCompletionSource();
        h.Speech.StartAsync(Arg.Any<Settings>()).Returns(_ => asynchronousStart ? startGate.Task : throw new IOException()); h.Speech.StopAsync().Returns(_ => asynchronousStop ? cleanup.Task : throw new IOException());
        // Act
        var start = h.Model.StartAsync(); if (asynchronousStart) startGate.SetException(new IOException()); h.Pump();
        // Assert
        Assert.Equal(1, h.Speech.ReceivedCalls().Count(c => c.GetMethodInfo().Name == "StopAsync"));
        if (asynchronousStop) { cleanup.SetException(new IOException()); h.Pump(); }
        MainViewModelTestHarness.Completed(start); await start; Assert.True(h.Model.HasStopError && h.Model.IsFrozen && h.Model.CanStop); Assert.False(h.Model.CanEdit || h.Model.CanStart || h.Model.IsStopPending);
        h.Speech.Configure().StopAsync().Returns(Task.CompletedTask); var retry = h.Model.StopAsync(); h.Pump(); MainViewModelTestHarness.Completed(retry); await retry;
        await h.Speech.Received(2).StopAsync(); Assert.False(h.Model.IsFrozen || h.Model.HasStopError); Assert.True(h.Model.CanEdit);
    }

    /// <summary>Idle stop is inert; all pending requests and repeated Ended callbacks reuse the identical task.</summary>
    [Fact]
    public async Task StopAsync_IdleIsNoOpAndPendingCallsReturnSameTaskAsync()
    {
        // Arrange
        using var h = new MainViewModelTestHarness(Valid); await h.InitializeAsync();
        // Act
        await h.Model.StopAsync();
        // Assert
        await h.Speech.DidNotReceive().StopAsync(); Assert.Empty(h.Model.Activity);
        await h.Model.StartAsync(); var gate = new TaskCompletionSource(); h.Speech.StopAsync().Returns(gate.Task);
        var first = h.Model.StopAsync(); var second = h.Model.StopAsync(); h.Pump(); var third = h.Model.StopAsync(); h.End("one"); h.End("two"); h.Pump();
        Assert.Same(first, second); Assert.Same(first, third); Assert.Same(first, h.Model.StopAsync()); Assert.True(h.Model.IsStopPending); Assert.False(h.Model.CanStop); await h.Speech.Received(1).StopAsync();
        gate.SetResult(); h.Pump(); MainViewModelTestHarness.Completed(first); await first; Assert.False(h.Model.IsFrozen || h.Model.IsStopPending);
    }

    /// <summary>A stop requested during pending start waits for start, rejects results immediately and cannot self-await after failure.</summary>
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task StopAsync_DuringPendingStart_WaitsForStartBeforeSpeechStopAsync(bool failStart)
    {
        // Arrange
        using var h = new MainViewModelTestHarness(Valid); await h.InitializeAsync(); var startGate = new TaskCompletionSource(); var stopGate = new TaskCompletionSource(); h.Speech.StartAsync(Arg.Any<Settings>()).Returns(startGate.Task); h.Speech.StopAsync().Returns(stopGate.Task); var start = h.Model.StartAsync();
        // Act
        var stop = h.Model.StopAsync(); h.Pump();
        // Assert
        await h.Speech.DidNotReceive().StopAsync(); Assert.False(stop.IsCompleted); h.Translate(); Assert.Empty(h.Model.Translations);
        if (failStart) startGate.SetException(new IOException()); else startGate.SetResult(); h.Pump(); await h.Speech.Received(1).StopAsync(); Assert.DoesNotContain("翻訳中", MainViewModelTestHarness.Messages(h.Model)); Assert.Same(stop, h.Model.StopAsync());
        stopGate.SetResult(); h.Pump(); MainViewModelTestHarness.Completed(start); MainViewModelTestHarness.Completed(stop); await start; await stop; Assert.False(h.Model.IsFrozen || h.Model.HasStopError);
    }

    /// <summary>Stop success wins an incomplete clock and a completed tie without warning or retained locks.</summary>
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task StopAsync_CompletedStop_DoesNotWarnAndUnfreezesAsync(bool completedTie)
    {
        // Arrange
        using var h = new MainViewModelTestHarness(Valid); await h.InitializeAsync(); await h.Model.StartAsync(); var gate = new TaskCompletionSource(); h.Speech.StopAsync().Returns(completedTie ? Task.CompletedTask : gate.Task); if (completedTie) h.Delay.SetResult();
        // Act
        var stop = h.Model.StopAsync(); h.Pump(); if (!completedTie) { gate.SetResult(); h.Pump(); } MainViewModelTestHarness.Completed(stop); await stop;
        // Assert
        Assert.Equal("停止", h.Model.Status); Assert.False(h.Model.IsFrozen || h.Model.HasStopError || h.Model.CanStop || h.Model.IsStopPending); Assert.True(h.Model.CanStart && h.Model.CanEdit); Assert.DoesNotContain(MainViewModelTestHarness.Messages(h.Model), s => s.Contains("10秒")); Assert.Equal(new[] { TimeSpan.FromSeconds(10) }, h.Delays);
    }

    /// <summary>The injected exact ten-second clock warns without canceling or completing the one pending stop.</summary>
    [Fact]
    public async Task StopAsync_TenSecondDelayWins_WarnsButContinuesFrozenAndSinglePendingAsync()
    {
        // Arrange
        using var h = new MainViewModelTestHarness(Valid); await h.InitializeAsync(); await h.Model.StartAsync(); var gate = new TaskCompletionSource(); h.Speech.StopAsync().Returns(gate.Task);
        // Act
        var stop = h.Model.StopAsync(); h.Pump(); h.Delay.SetResult(); h.Pump();
        // Assert
        Assert.Equal(new[] { TimeSpan.FromSeconds(10) }, h.Delays); Assert.Contains("10秒", h.Model.Status); Assert.False(stop.IsCompleted); Assert.True(h.Model.IsFrozen && h.Model.IsStopPending); Assert.False(h.Model.CanEdit || h.Model.CanStart || h.Model.CanStop); Assert.Same(stop, h.Model.StopAsync()); await h.Model.StartAsync(); await h.Speech.Received(1).StartAsync(Valid); await h.Speech.Received(1).StopAsync();
        gate.SetResult(); h.Pump(); MainViewModelTestHarness.Completed(stop); await stop; Assert.False(h.Model.IsFrozen || h.Model.HasStopError); Assert.True(h.Model.CanStart); await h.Speech.Received(1).StopAsync();
    }

    /// <summary>Stop failure before or after a warning preserves the lock; retries are fresh and clear errors only on success.</summary>
    [Theory]
    [InlineData(false, false, false)] [InlineData(false, false, true)] [InlineData(true, false, false)] [InlineData(true, false, true)] [InlineData(true, true, false)] [InlineData(true, true, true)]
    public async Task StopAsync_Failure_RemainsFrozenRejectsStartAndPermitsFreshRetryAsync(bool asynchronous, bool warning, bool retryFails)
    {
        // Arrange
        using var h = new MainViewModelTestHarness(Valid); await h.InitializeAsync(); await h.Model.StartAsync(); var gate = new TaskCompletionSource(); h.Speech.StopAsync().Returns(_ => asynchronous ? gate.Task : throw new IOException());
        // Act
        var stop = h.Model.StopAsync(); h.Pump(); if (warning) { h.Delay.SetResult(); h.Pump(); Assert.Contains("10秒", h.Model.Status); }
        if (asynchronous) { gate.SetException(new IOException()); h.Pump(); } MainViewModelTestHarness.Completed(stop); await stop;
        // Assert
        Assert.True(h.Model.HasStopError && h.Model.IsFrozen && h.Model.CanStop); Assert.False(h.Model.CanStart || h.Model.CanEdit || h.Model.IsStopPending); h.Translate(); Assert.Empty(h.Model.Translations); await h.Model.StartAsync(); await h.Speech.Received(1).StartAsync(Valid);
        h.Speech.Configure().StopAsync().Returns(retryFails ? Task.FromException(new IOException()) : Task.CompletedTask); var retry = h.Model.StopAsync(); Assert.NotSame(stop, retry); h.Pump(); MainViewModelTestHarness.Completed(retry); await retry; await h.Speech.Received(2).StopAsync(); Assert.Equal(retryFails, h.Model.IsFrozen); Assert.Equal(retryFails, h.Model.HasStopError); Assert.Equal(!retryFails, h.Model.CanEdit); Assert.Equal(retryFails, h.Model.CanStop);
        if (retryFails) { h.Speech.Configure().StopAsync().Returns(Task.CompletedTask); var final = h.Model.StopAsync(); h.Pump(); MainViewModelTestHarness.Completed(final); await final; Assert.False(h.Model.HasStopError || h.Model.IsFrozen); }
        await h.Model.StartAsync(); Assert.False(h.Model.HasStopError); await h.Speech.Received(2).StartAsync(Valid);
    }

    /// <summary>A synchronous Ended event raised inside stop cannot reenter before task assignment.</summary>
    [Fact]
    public async Task StopAsync_ReentrantEndedEvent_DoesNotCreateSecondStopAsync()
    {
        // Arrange
        using var h = new MainViewModelTestHarness(Valid); await h.InitializeAsync(); await h.Model.StartAsync(); var gate = new TaskCompletionSource(); h.Speech.StopAsync().Returns(_ => { h.End("reentrant"); h.End("again"); return gate.Task; });
        // Act
        var stop = h.Model.StopAsync(); h.Pump();
        // Assert
        Assert.Same(stop, h.Model.StopAsync()); await h.Speech.Received(1).StopAsync(); Assert.Contains("reentrant", MainViewModelTestHarness.Messages(h.Model)); Assert.Contains("again", MainViewModelTestHarness.Messages(h.Model));
        gate.SetResult(); h.Pump(); MainViewModelTestHarness.Completed(stop); await stop; Assert.False(h.Model.IsFrozen || h.Model.IsStopPending);
    }
}
