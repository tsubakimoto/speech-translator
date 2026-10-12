using Tsubakimoto.SpeechTranslator.WinUIApp.Core;

namespace Tsubakimoto.SpeechTranslator.WinUIApp.Tests;

/// <summary>Tests durable view-model state and deterministic speech lifecycle contracts.</summary>
public sealed partial class MainViewModelTests
{
    private static readonly Settings Valid = new("japaneast", "committed-key");

    /// <summary>Construction neither initializes nor touches persistence or speech.</summary>
    [Fact]
    public void InitialState_IsNotEditableBeforeInitialization()
    {
        // Arrange
        using var h = new MainViewModelTestHarness();
        // Act
        var m = h.Model;
        // Assert
        Assert.Equal(new Settings(), m.CommittedSettings);
        Assert.Equal(("", "", "", "en-US", "ja-JP", "", "停止", ""), (m.Region, m.Key, m.Filename, m.Source, m.Target, m.Folder, m.Status, m.SettingsMessage));
        Assert.Empty(m.Activity); Assert.Empty(m.Translations);
        Assert.False(m.IsInitialized || m.CanEdit || m.CanStart || m.CanStop || m.IsStopPending || m.IsFrozen || m.IsPersisting || m.HasRecoveryError || m.HasStopError || m.HasUnsavedChanges || m.IsSettingsVisible);
        Assert.Empty(h.Store.ReceivedCalls()); Assert.DoesNotContain(h.Speech.ReceivedCalls(), c => c.GetMethodInfo().Name is "StartAsync" or "StopAsync");
    }

    /// <summary>Draft, durable, persistence, recovery and freeze changes refresh observable capabilities.</summary>
    [Fact]
    public async Task PropertyChanges_RefreshCapabilitiesAndDraftStateAsync()
    {
        // Arrange
        using var h = new MainViewModelTestHarness(Valid);
        var names = new List<string?>(); h.Model.PropertyChanged += (_, e) => names.Add(e.PropertyName);
        // Act
        await h.InitializeAsync();
        // Assert
        Assert.Contains("CanEdit", names); Assert.Contains("CanStart", names); Assert.True(h.Model.CanStart);
        names.Clear(); h.Model.Region = "westus"; h.Model.Key = "draft";
        Assert.Contains("HasUnsavedChanges", names); Assert.True(h.Model.HasUnsavedChanges); Assert.True(h.Model.CanStart);
        names.Clear(); var save = new TaskCompletionSource(); h.Store.SaveAsync(Arg.Any<Settings>()).Returns(save.Task);
        var pending = h.Model.SaveSettingsAsync();
        Assert.Contains("IsPersisting", names); Assert.Contains("CanEdit", names); Assert.False(h.Model.CanEdit);
        save.SetResult(); h.Pump(); MainViewModelTestHarness.Completed(pending); await pending;
        Assert.Contains("CommittedSettings", names); Assert.False(h.Model.HasUnsavedChanges); Assert.True(h.Model.CanStart);
        names.Clear(); await h.Model.StartAsync();
        Assert.Contains("CanStop", names); Assert.Contains("IsStopPending", names); Assert.False(h.Model.CanEdit); Assert.True(h.Model.CanStop);
        var stop = h.Model.StopAsync(); h.Pump(); MainViewModelTestHarness.Completed(stop); await stop;
        Assert.True(h.Model.CanEdit); Assert.False(h.Model.CanStop);
        names.Clear(); h.Model.HasRecoveryError = true;
        Assert.Contains("CanEdit", names); Assert.Contains("CanStart", names); Assert.False(h.Model.CanEdit || h.Model.CanStart);
    }

    /// <summary>Final stop getters are consistent; captures completion notifications without inventing a UI guarantee.</summary>
    [Fact]
    public async Task StopAsync_CompletionNotification_ReportsObservedPendingStateAsync()
    {
        // Arrange
        using var h = new MainViewModelTestHarness(Valid); await h.InitializeAsync(); await h.Model.StartAsync();
        var observed = new List<(bool Pending, bool CanStop)>();
        h.Model.PropertyChanged += (_, e) => { if (e.PropertyName == "IsStopPending") observed.Add((h.Model.IsStopPending, h.Model.CanStop)); };
        // Act
        var stop = h.Model.StopAsync(); h.Pump(); MainViewModelTestHarness.Completed(stop); await stop;
        // Assert
        Assert.False(h.Model.IsStopPending || h.Model.CanStop); Assert.True(h.Model.CanEdit);
        Assert.Contains((true, false), observed);
    }
}
