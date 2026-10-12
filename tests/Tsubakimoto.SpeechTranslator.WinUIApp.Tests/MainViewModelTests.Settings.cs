using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Tsubakimoto.SpeechTranslator.WinUIApp.Core;

namespace Tsubakimoto.SpeechTranslator.WinUIApp.Tests;

public sealed partial class MainViewModelTests
{
    /// <summary>Environment fallback supplies only credentials after a successful empty load.</summary>
    [Theory]
    [InlineData(false, "absent")]
    [InlineData(false, "invalid")]
    [InlineData(false, "valid")]
    [InlineData(true, "absent")]
    [InlineData(true, "invalid")]
    [InlineData(true, "valid")]
    [InlineData(false, "final-lf")]
    public async Task InitializeAsync_EmptySuccessfulLoad_UsesDefaultsOrValidEnvironmentAsync(bool preferences, string environment)
    {
        // Arrange
        var saved = preferences ? new Settings(Source: "ja-JP", Target: "en-US", Folder: "saved-folder") : null;
        using var h = new MainViewModelTestHarness(saved);
        var env = environment switch { "valid" => new Settings("westus", "env-key", "ja-JP", "ja-JP", "env-folder"), "invalid" => new Settings("X", "env-key"), "final-lf" => new Settings("ab\n", "env-key"), _ => null };
        // Act
        await h.Model.InitializeAsync(env); await h.Model.InitializeAsync(Valid);
        // Assert
        var expected = (saved ?? new Settings()) with { Region = environment == "valid" ? "westus" : "", Key = environment == "valid" ? "env-key" : "" };
        Assert.Equal(expected, h.Model.CommittedSettings); Assert.Equal("", h.Model.Filename);
        Assert.Equal(expected.Source, h.Model.Source); Assert.Equal(expected.Target, h.Model.Target); Assert.Equal(expected.Folder ?? "", h.Model.Folder);
        Assert.True(h.Model.IsInitialized && h.Model.CanEdit); Assert.Equal(environment == "valid", h.Model.CanStart);
        Assert.Equal(environment != "valid", h.Model.IsSettingsVisible); Assert.False(h.Model.HasRecoveryError || h.Model.HasUnsavedChanges);
        await h.Store.Received(1).LoadAsync(); await h.Store.DidNotReceive().SaveAsync(Arg.Any<Settings>()); await h.Store.DidNotReceive().RecoverAsync(Arg.Any<Settings>());
    }

    /// <summary>Nonempty saved fields never silently switch to environment credentials.</summary>
    [Theory]
    [InlineData("japaneast", "saved-key", true)]
    [InlineData("japaneast", "", false)]
    [InlineData("", "saved-key", false)]
    [InlineData("INVALID", "saved-key", false)]
    [InlineData("ab", "bad key", false)]
    [InlineData("ab\n", "saved-key", false)]
    public async Task InitializeAsync_SavedOrPartialInvalidCredentials_DoNotUseEnvironmentAsync(string region, string key, bool valid)
    {
        // Arrange
        var saved = new Settings(region, key, "ja-JP", "ja-JP", "stored-folder"); using var h = new MainViewModelTestHarness(saved);
        // Act
        await h.Model.InitializeAsync(Valid);
        // Assert
        Assert.Equal(saved, h.Model.CommittedSettings); Assert.Equal(region, h.Model.Region); Assert.Equal(key, h.Model.Key);
        Assert.Equal("", h.Model.Filename); Assert.Equal("stored-folder", h.Model.Folder); Assert.Equal("ja-JP", h.Model.Source); Assert.Equal("ja-JP", h.Model.Target);
        Assert.Equal(valid, h.Model.CanStart); Assert.Equal(!valid, h.Model.IsSettingsVisible);
        await h.Store.DidNotReceive().SaveAsync(Arg.Any<Settings>()); await h.Store.DidNotReceive().RecoverAsync(Arg.Any<Settings>());
    }

    private static Exception Failure(string kind) => kind switch { "crypto" => new CryptographicException("decrypt"), "sqlite" => new SqliteException("corrupt", 11), _ => new IOException("storage") };

    /// <summary>Read failures lock editing and require explicit recovery without environment fallback.</summary>
    [Theory]
    [InlineData("io", false)] [InlineData("io", true)] [InlineData("crypto", true)] [InlineData("sqlite", true)]
    public async Task InitializeAsync_ReadOrDecryptFailure_RequiresExplicitRecoveryWithoutFallbackAsync(string kind, bool asynchronous)
    {
        // Arrange
        using var h = new MainViewModelTestHarness(); h.Store.LoadAsync().Returns(_ => asynchronous ? Task.FromException<Settings?>(Failure(kind)) : throw Failure(kind));
        // Act
        await h.Model.InitializeAsync(Valid); await h.Model.InitializeAsync(Valid);
        // Assert
        Assert.True(h.Model.IsInitialized && h.Model.HasRecoveryError && h.Model.IsSettingsVisible); Assert.False(h.Model.CanEdit || h.Model.CanStart);
        Assert.Equal(new Settings(), h.Model.CommittedSettings); Assert.Equal("", h.Model.Region); Assert.Equal("", h.Model.Key); Assert.Contains("復旧", h.Model.SettingsMessage);
        await h.Store.Received(1).LoadAsync(); await h.Store.DidNotReceive().SaveAsync(Arg.Any<Settings>()); await h.Store.DidNotReceive().RecoverAsync(Arg.Any<Settings>());
    }

    /// <summary>Trimmed credentials become committed only after the durable save gate completes.</summary>
    [Fact]
    public async Task SaveSettingsAsync_ValidDraft_CommitsOnlyAfterDurableSaveAsync()
    {
        // Arrange
        var old = Valid with { Source = "ja-JP", Target = "en-US", Folder = "old-folder" }; using var h = new MainViewModelTestHarness(old); await h.InitializeAsync();
        h.Model.Region = " \twestus\r\n"; h.Model.Key = " \t秘密-key\r\n"; h.Model.Filename = "not-persisted";
        var gate = new TaskCompletionSource(); h.Store.SaveAsync(Arg.Any<Settings>()).Returns(gate.Task);
        // Act
        var pending = h.Model.SaveSettingsAsync();
        // Assert
        Assert.True(h.Model.IsPersisting); Assert.False(h.Model.CanEdit || h.Model.CanStart); Assert.Equal(old, h.Model.CommittedSettings);
        await h.Store.Received(1).SaveAsync(old with { Region = "westus", Key = "秘密-key" });
        gate.SetResult(); h.Pump(); MainViewModelTestHarness.Completed(pending); Assert.True(await pending);
        Assert.Equal(old with { Region = "westus", Key = "秘密-key" }, h.Model.CommittedSettings); Assert.Equal("westus", h.Model.Region); Assert.Equal("秘密-key", h.Model.Key);
        Assert.False(h.Model.IsPersisting || h.Model.HasUnsavedChanges); Assert.True(h.Model.CanStart); Assert.Equal("not-persisted", h.Model.Filename);
        await h.Store.DidNotReceive().RecoverAsync(Arg.Any<Settings>());
    }

    /// <summary>Invalid credentials and public guard states do not write or normalize drafts.</summary>
    [Theory]
    [InlineData("invalid-region")] [InlineData("invalid-key")] [InlineData("uninitialized")] [InlineData("frozen")] [InlineData("persisting")] [InlineData("recovery")] [InlineData("committed-invalid")]
    public async Task SaveSettingsAsync_InvalidDraftOrGuard_DoesNotWriteAsync(string state)
    {
        // Arrange
        using var h = new MainViewModelTestHarness(state == "committed-invalid" ? new Settings() : Valid);
        if (state != "uninitialized") await h.InitializeAsync();
        h.Model.Region = state == "invalid-region" ? "BAD" : "westus"; h.Model.Key = state == "invalid-key" ? "bad key" : "draft-key";
        if (state == "frozen") await h.Model.StartAsync();
        if (state == "persisting") h.Model.IsPersisting = true;
        if (state == "recovery") h.Model.HasRecoveryError = true;
        var region = h.Model.Region; var key = h.Model.Key;
        // Act
        if (state == "committed-invalid")
        {
            // Assert
            Assert.False(h.Model.CanStart); Assert.True(h.Model.HasUnsavedChanges); return;
        }
        var result = await h.Model.SaveSettingsAsync();
        // Assert
        Assert.False(result); Assert.Equal(region, h.Model.Region); Assert.Equal(key, h.Model.Key); await h.Store.DidNotReceive().SaveAsync(Arg.Any<Settings>());
        if (state is "invalid-region" or "invalid-key") Assert.True(h.Model.CanStart);
    }

    /// <summary>Failed credential writes retain raw drafts and can be retried durably.</summary>
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task SaveSettingsAsync_Failure_RetainsRawDraftsAndCommittedSettingsThenRetryAsync(bool asynchronous)
    {
        // Arrange
        using var h = new MainViewModelTestHarness(Valid); await h.InitializeAsync(); h.Model.Region = " westus "; h.Model.Key = " new-key ";
        h.Store.SaveAsync(Arg.Any<Settings>()).Returns(_ => asynchronous ? Task.FromException(new IOException()) : throw new IOException());
        // Act
        var failed = await h.Model.SaveSettingsAsync();
        // Assert
        Assert.False(failed); Assert.Equal(Valid, h.Model.CommittedSettings); Assert.Equal(" westus ", h.Model.Region); Assert.Equal(" new-key ", h.Model.Key); Assert.True(h.Model.HasUnsavedChanges); Assert.False(h.Model.IsPersisting); Assert.Contains("失敗", h.Model.SettingsMessage);
        h.Store.SaveAsync(Arg.Any<Settings>()).Returns(Task.CompletedTask); Assert.True(await h.Model.SaveSettingsAsync());
        await h.Store.Received(2).SaveAsync(Valid with { Region = "westus", Key = "new-key" }); Assert.False(h.Model.HasUnsavedChanges); Assert.Equal("westus", h.Model.Region); Assert.Equal("new-key", h.Model.Key);
    }

    /// <summary>Cancel restores credentials alone and never saves or changes navigation.</summary>
    [Fact]
    public async Task CancelSettings_RestoresCredentialsOnlyWithoutWritingAsync()
    {
        // Arrange
        using var h = new MainViewModelTestHarness(Valid); await h.InitializeAsync(); Assert.True(await h.Model.SavePreferencesAsync("ja-JP", "en-US", null)); h.Store.ClearReceivedCalls();
        h.Model.Region = "draft"; h.Model.Key = "draft"; h.Model.Filename = "chosen"; h.Model.IsSettingsVisible = true; h.Model.HasRecoveryError = true;
        // Act
        h.Model.CancelSettings();
        // Assert
        Assert.Equal(Valid.Region, h.Model.Region); Assert.Equal(Valid.Key, h.Model.Key); Assert.False(h.Model.HasUnsavedChanges); Assert.Equal("ja-JP", h.Model.Source); Assert.Equal("en-US", h.Model.Target); Assert.Equal("chosen", h.Model.Filename); Assert.True(h.Model.IsSettingsVisible && h.Model.HasRecoveryError); Assert.Empty(h.Store.ReceivedCalls());
    }

    private static async Task RequireRecoveryAsync(MainViewModelTestHarness h)
    {
        h.Store.LoadAsync().Returns(Task.FromException<Settings?>(new CryptographicException())); await h.Model.InitializeAsync(Valid); h.Model.Region = " westus "; h.Model.Key = " recovered-key ";
    }

    /// <summary>Explicit recovery retains the lock until durable completion and reports the backup.</summary>
    [Fact]
    public async Task RecoverAsync_ExplicitValidDraft_CommitsAndReportsBackupAsync()
    {
        // Arrange
        using var h = new MainViewModelTestHarness(); await RequireRecoveryAsync(h); var message = h.Model.SettingsMessage; var gate = new TaskCompletionSource<string>(); h.Store.RecoverAsync(Arg.Any<Settings>()).Returns(gate.Task);
        // Act
        var pending = h.Model.RecoverAsync();
        // Assert
        Assert.True(h.Model.IsPersisting && h.Model.HasRecoveryError); Assert.False(h.Model.CanStart || h.Model.CanEdit); Assert.Equal(new Settings(), h.Model.CommittedSettings); Assert.Equal(message, h.Model.SettingsMessage);
        Assert.False(await h.Model.RecoverAsync()); Assert.False(await h.Model.SaveSettingsAsync()); Assert.False(await h.Model.SavePreferencesAsync("ja-JP", "en-US", null));
        gate.SetResult("original.backup.db"); h.Pump(); MainViewModelTestHarness.Completed(pending); Assert.True(await pending);
        Assert.Equal(new Settings("westus", "recovered-key"), h.Model.CommittedSettings); Assert.Equal("westus", h.Model.Region); Assert.Equal("recovered-key", h.Model.Key); Assert.Contains("original.backup.db", h.Model.SettingsMessage); Assert.False(h.Model.HasRecoveryError || h.Model.IsPersisting || h.Model.HasUnsavedChanges); Assert.True(h.Model.CanStart);
        await h.Store.Received(1).RecoverAsync(new Settings("westus", "recovered-key")); await h.Store.DidNotReceive().SaveAsync(Arg.Any<Settings>());
    }

    /// <summary>Recovery rejects invalid drafts and all public guard combinations without store calls.</summary>
    [Theory]
    [InlineData("invalid-region")] [InlineData("invalid-key")] [InlineData("no-error")] [InlineData("frozen")] [InlineData("persisting")]
    public async Task RecoverAsync_InvalidDraftOrGuard_DoesNotCallStoreAsync(string state)
    {
        // Arrange
        using var h = new MainViewModelTestHarness(); await RequireRecoveryAsync(h);
        if (state == "invalid-region") h.Model.Region = "X";
        if (state == "invalid-key") h.Model.Key = "bad key";
        if (state == "no-error") h.Model.HasRecoveryError = false;
        if (state == "frozen") h.Model.IsFrozen = true; // Generated public state, not private-field reflection.
        if (state == "persisting") h.Model.IsPersisting = true;
        var raw = (h.Model.Region, h.Model.Key);
        // Act
        var result = await h.Model.RecoverAsync();
        // Assert
        Assert.False(result); Assert.Equal(raw, (h.Model.Region, h.Model.Key)); await h.Store.DidNotReceive().RecoverAsync(Arg.Any<Settings>()); await h.Store.DidNotReceive().SaveAsync(Arg.Any<Settings>());
    }

    /// <summary>Recovery failures retain the lock and drafts; a fresh retry alone unlocks editing.</summary>
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task RecoverAsync_Failure_RetainsDraftAndRecoveryLockThenRetryAsync(bool asynchronous)
    {
        // Arrange
        using var h = new MainViewModelTestHarness(); await RequireRecoveryAsync(h); h.Store.RecoverAsync(Arg.Any<Settings>()).Returns(_ => asynchronous ? Task.FromException<string>(new IOException()) : throw new IOException());
        // Act
        var failed = await h.Model.RecoverAsync();
        // Assert
        Assert.False(failed); Assert.True(h.Model.HasRecoveryError); Assert.False(h.Model.IsPersisting || h.Model.CanStart || h.Model.CanEdit); Assert.Equal(new Settings(), h.Model.CommittedSettings); Assert.Equal(" westus ", h.Model.Region); Assert.Equal(" recovered-key ", h.Model.Key); Assert.Contains("失敗", h.Model.SettingsMessage);
        h.Store.RecoverAsync(Arg.Any<Settings>()).Returns(Task.FromResult("retry.backup")); Assert.True(await h.Model.RecoverAsync());
        await h.Store.Received(2).RecoverAsync(new Settings("westus", "recovered-key")); await h.Store.DidNotReceive().SaveAsync(Arg.Any<Settings>()); Assert.True(h.Model.CanStart); Assert.False(h.Model.HasRecoveryError || h.Model.HasUnsavedChanges);
    }

    /// <summary>All language pairs persist committed credentials while keeping unsaved drafts untouched.</summary>
    [Theory]
    [InlineData("en-US", "en-US", false)] [InlineData("en-US", "ja-JP", false)] [InlineData("ja-JP", "en-US", false)] [InlineData("ja-JP", "ja-JP", false)]
    [InlineData("en-US", "en-US", true)] [InlineData("en-US", "ja-JP", true)] [InlineData("ja-JP", "en-US", true)] [InlineData("ja-JP", "ja-JP", true)]
    public async Task SavePreferencesAsync_AllLanguagePairsAndNullFolder_PersistsImmediatelyAsync(string source, string target, bool clear)
    {
        // Arrange
        using var directory = new TestDirectory(); using var h = new MainViewModelTestHarness(Valid with { Folder = directory.Root }); await h.InitializeAsync(); h.Model.Region = " raw-region "; h.Model.Key = " raw-key "; h.Model.Filename = "not-stored"; var folder = clear ? null : directory.Root;
        // Act
        var result = await h.Model.SavePreferencesAsync(source, target, folder);
        // Assert
        Assert.True(result); var expected = Valid with { Source = source, Target = target, Folder = folder }; Assert.Equal(expected, h.Model.CommittedSettings); await h.Store.Received(1).SaveAsync(expected);
        Assert.Equal(source, h.Model.Source); Assert.Equal(target, h.Model.Target); Assert.Equal(folder ?? "", h.Model.Folder); Assert.Equal(" raw-region ", h.Model.Region); Assert.Equal(" raw-key ", h.Model.Key); Assert.True(h.Model.HasUnsavedChanges); Assert.Equal("not-stored", h.Model.Filename); Assert.False(h.Model.IsPersisting);
    }

    /// <summary>Each invalid selection restores all displayed preferences without persistence.</summary>
    [Theory]
    [InlineData("source", false)] [InlineData("target", false)] [InlineData("empty", false)] [InlineData("relative", false)] [InlineData("root-relative", false)] [InlineData("missing", false)] [InlineData("file", false)] [InlineData("missing", true)]
    public async Task SavePreferencesAsync_InvalidSelection_RollsBackWithoutStoreCallAsync(string kind, bool defaultFolder)
    {
        // Arrange
        using var directory = new TestDirectory(); var file = directory.GetPath("file"); File.WriteAllText(file, "owned"); var old = Valid with { Folder = defaultFolder ? null : directory.Root }; using var h = new MainViewModelTestHarness(old); await h.InitializeAsync();
        h.Model.Source = "attempted"; h.Model.Target = "attempted"; h.Model.Folder = "attempted";
        var folder = kind switch { "empty" => "", "relative" => "relative", "root-relative" => "\\relative", "missing" => directory.GetPath("missing"), "file" => file, _ => directory.Root };
        // Act
        var result = await h.Model.SavePreferencesAsync(kind == "source" ? "fr-FR" : "ja-JP", kind == "target" ? "fr-FR" : "en-US", folder);
        // Assert
        Assert.False(result); Assert.Equal(old, h.Model.CommittedSettings); Assert.Equal(old.Source, h.Model.Source); Assert.Equal(old.Target, h.Model.Target); Assert.Equal(old.Folder ?? "", h.Model.Folder); await h.Store.DidNotReceive().SaveAsync(Arg.Any<Settings>());
    }

    /// <summary>Failed preference writes roll back every preference without discarding credential drafts.</summary>
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task SavePreferencesAsync_FailedWrite_RollsBackAllPreferencesAndRetainsCredentialDraftsAsync(bool asynchronous)
    {
        // Arrange
        using var directory = new TestDirectory(); var old = Valid with { Folder = directory.Root }; using var h = new MainViewModelTestHarness(old); await h.InitializeAsync(); h.Model.Region = " raw "; h.Model.Key = " draft "; h.Model.Source = "ja-JP"; h.Model.Target = "en-US"; h.Model.Folder = "";
        h.Store.SaveAsync(Arg.Any<Settings>()).Returns(_ => asynchronous ? Task.FromException(new IOException()) : throw new IOException());
        // Act
        var failed = await h.Model.SavePreferencesAsync("ja-JP", "en-US", null);
        // Assert
        Assert.False(failed); Assert.Equal(old, h.Model.CommittedSettings); Assert.Equal("en-US", h.Model.Source); Assert.Equal("ja-JP", h.Model.Target); Assert.Equal(directory.Root, h.Model.Folder); Assert.Equal(" raw ", h.Model.Region); Assert.Equal(" draft ", h.Model.Key); Assert.False(h.Model.IsPersisting); Assert.Contains("失敗", h.Model.SettingsMessage);
        h.Store.SaveAsync(Arg.Any<Settings>()).Returns(Task.CompletedTask); Assert.True(await h.Model.SavePreferencesAsync("ja-JP", "en-US", null)); await h.Store.Received(2).SaveAsync(Valid with { Source = "ja-JP", Target = "en-US" }); Assert.True(h.Model.HasUnsavedChanges);
    }

    /// <summary>Pending preference writes reject all reentry and start; their captured selection wins or rolls back.</summary>
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task SavePreferencesAsync_PendingWrite_BlocksReentryAndStartAsync(bool fail)
    {
        // Arrange
        using var h = new MainViewModelTestHarness(Valid); await h.InitializeAsync(); h.Model.Region = "westus"; h.Model.Key = "draft"; var gate = new TaskCompletionSource(); h.Store.SaveAsync(Arg.Any<Settings>()).Returns(gate.Task);
        // Act
        var pending = h.Model.SavePreferencesAsync("ja-JP", "en-US", null);
        // Assert
        Assert.True(h.Model.IsPersisting); Assert.False(h.Model.CanStart || h.Model.CanEdit);
        Assert.False(await h.Model.SavePreferencesAsync("en-US", "en-US", null)); Assert.False(await h.Model.SaveSettingsAsync()); Assert.False(await h.Model.RecoverAsync()); await h.Model.StartAsync();
        await h.Store.Received(1).SaveAsync(Valid with { Source = "ja-JP", Target = "en-US" }); h.Recording.DidNotReceive().Validate(Arg.Any<string?>(), Arg.Any<string>()); await h.Speech.DidNotReceive().StartAsync(Arg.Any<Settings>());
        Assert.Equal(Valid, h.Model.CommittedSettings); if (fail) gate.SetException(new IOException()); else gate.SetResult(); h.Pump(); MainViewModelTestHarness.Completed(pending); Assert.Equal(!fail, await pending);
        Assert.Equal(fail ? Valid : Valid with { Source = "ja-JP", Target = "en-US" }, h.Model.CommittedSettings); Assert.Equal(fail ? "en-US" : "ja-JP", h.Model.Source); Assert.Equal(fail ? "ja-JP" : "en-US", h.Model.Target); Assert.Equal("westus", h.Model.Region); Assert.Equal("draft", h.Model.Key); Assert.False(h.Model.IsPersisting); Assert.True(h.Model.CanStart);
    }
}
