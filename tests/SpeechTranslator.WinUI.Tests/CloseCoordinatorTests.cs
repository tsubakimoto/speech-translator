using NSubstitute.Extensions;
using SpeechTranslatorWinUI.Core;

namespace SpeechTranslator.WinUI.Tests;

/// <summary>Deterministic close-dialog and resource-release coordination checks.</summary>
public sealed class CloseCoordinatorTests
{
    private static readonly Settings Valid = new("japaneast", new string('a', 32));

    /// <summary>Canceling an unsaved draft leaves speech untouched.</summary>
    [Fact]
    public async Task TryCloseAsync_DraftCanceled_DoesNotStopAsync()
    {
        // Arrange
        using var h = new MainViewModelTestHarness(Valid); await h.InitializeAsync(); await h.Model.StartAsync();
        var close = new CloseCoordinator(h.Model, () => Task.FromResult(false), () => throw new InvalidOperationException(), _ => throw new InvalidOperationException());
        // Act
        var result = await close.TryCloseAsync();
        // Assert
        Assert.False(result); Assert.True(h.Model.IsFrozen); await h.Speech.DidNotReceive().StopAsync();
    }

    /// <summary>Idle close needs no resource-release operation.</summary>
    [Fact]
    public async Task TryCloseAsync_Idle_ClosesWithoutStopAsync()
    {
        // Arrange
        using var h = new MainViewModelTestHarness(Valid); await h.InitializeAsync();
        var close = new CloseCoordinator(h.Model, () => Task.FromResult(true), () => throw new InvalidOperationException(), _ => throw new InvalidOperationException());
        // Act
        var result = await close.TryCloseAsync();
        // Assert
        Assert.True(result); await h.Speech.DidNotReceive().StopAsync();
    }

    /// <summary>Repeated close and an existing stop reuse the same pending native release.</summary>
    [Fact]
    public async Task TryCloseAsync_PendingStopAndDuplicateClose_WaitsForOneStopAsync()
    {
        // Arrange
        using var h = new MainViewModelTestHarness(Valid); await h.InitializeAsync(); await h.Model.StartAsync();
        var stopGate = new TaskCompletionSource(); h.Speech.StopAsync().Returns(stopGate.Task); var stop = h.Model.StopAsync(); h.Pump();
        var delay = new TaskCompletionSource(); var close = new CloseCoordinator(h.Model, () => Task.FromResult(true), () => throw new InvalidOperationException(), _ => delay.Task);
        // Act
        var attempt = close.TryCloseAsync(); Assert.False(await close.TryCloseAsync()); stopGate.SetResult(); h.Pump();
        // Assert
        MainViewModelTestHarness.Completed(attempt); Assert.True(await attempt); Assert.True(stop.IsCompleted); await h.Speech.Received(1).StopAsync(); Assert.False(h.Model.IsFrozen);
    }

    /// <summary>Deadline choices never cancel or duplicate the ongoing stop.</summary>
    [Theory]
    [InlineData(2, false)]
    [InlineData(1, true)]
    public async Task TryCloseAsync_Timeout_RespectsChoiceWithoutDuplicatingStopAsync(int decision, bool expected)
    {
        // Arrange
        using var h = new MainViewModelTestHarness(Valid); await h.InitializeAsync(); await h.Model.StartAsync();
        var stopGate = new TaskCompletionSource(); h.Speech.StopAsync().Returns(stopGate.Task); var delay = new TaskCompletionSource();
        var durations = new List<TimeSpan>(); var close = new CloseCoordinator(h.Model, () => Task.FromResult(true), () => Task.FromResult((CloseDecision)decision), d => { durations.Add(d); return delay.Task; });
        // Act
        var attempt = close.TryCloseAsync(); h.Pump(); Assert.False(attempt.IsCompleted); delay.SetResult(); h.Pump();
        // Assert
        MainViewModelTestHarness.Completed(attempt); Assert.Equal(expected, await attempt); Assert.Equal(new[] { TimeSpan.FromSeconds(10) }, durations); Assert.True(h.Model.IsStopPending); await h.Speech.Received(1).StopAsync();
        stopGate.SetResult(); h.Pump();
    }

    /// <summary>Only a completed failed stop permits a fresh retry.</summary>
    [Fact]
    public async Task TryCloseAsync_StopFails_RetryThenClosesAsync()
    {
        // Arrange
        using var h = new MainViewModelTestHarness(Valid); await h.InitializeAsync(); await h.Model.StartAsync();
        h.Speech.StopAsync().Returns(Task.FromException(new IOException("stop"))); var decisions = 0;
        var close = new CloseCoordinator(h.Model, () => Task.FromResult(true), () => { decisions++; h.Speech.Configure().StopAsync().Returns(Task.CompletedTask); return Task.FromResult(CloseDecision.Retry); }, _ => new TaskCompletionSource().Task);
        // Act
        var attempt = close.TryCloseAsync(); h.Pump();
        // Assert
        MainViewModelTestHarness.Completed(attempt); Assert.True(await attempt); Assert.Equal(1, decisions); await h.Speech.Received(2).StopAsync(); Assert.False(h.Model.IsFrozen);
    }
}
