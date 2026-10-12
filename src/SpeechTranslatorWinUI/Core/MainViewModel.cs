using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
namespace SpeechTranslatorWinUI.Core;
/// <summary>UI-independent state machine; all UI mutations run on the supplied dispatcher.</summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly ISettingsStore _store;
    private readonly ISpeechSession _speech;
    private readonly IRecordingWriter _recording;
    private readonly Action<Action> _dispatch;
    private readonly Func<TimeSpan, Task> _delayAsync;
    private Settings _settings = new();
    private Task? _startTask;
    private Task? _stopTask;
    private string? _recordingPath;
    private bool _isAcceptingResults;
    /// <summary>Observable Region state.</summary>
    [ObservableProperty] public partial string Region { get; set; } = "";
    /// <summary>Observable Key state.</summary>
    [ObservableProperty] public partial string Key { get; set; } = "";
    /// <summary>Observable Filename state.</summary>
    [ObservableProperty] public partial string Filename { get; set; } = "";
    /// <summary>Observable Source state.</summary>
    [ObservableProperty] public partial string Source { get; set; } = "en-US";
    /// <summary>Observable Target state.</summary>
    [ObservableProperty] public partial string Target { get; set; } = "ja-JP";
    /// <summary>Observable Folder state.</summary>
    [ObservableProperty] public partial string Folder { get; set; } = "";
    /// <summary>Observable Status state.</summary>
    [ObservableProperty] public partial string Status { get; set; } = "停止";
    /// <summary>Observable SettingsMessage state.</summary>
    [ObservableProperty] public partial string SettingsMessage { get; set; } = "";
    /// <summary>Observable IsFrozen state.</summary>
    [ObservableProperty] public partial bool IsFrozen { get; set; }
    /// <summary>Observable IsPersisting state.</summary>
    [ObservableProperty] public partial bool IsPersisting { get; set; }
    /// <summary>Observable HasRecoveryError state.</summary>
    [ObservableProperty] public partial bool HasRecoveryError { get; set; }
    /// <summary>Observable HasStopError state.</summary>
    [ObservableProperty] public partial bool HasStopError { get; set; }
    /// <summary>Observable IsSettingsVisible state.</summary>
    [ObservableProperty] public partial bool IsSettingsVisible { get; set; }
    /// <summary>Observable IsInitialized state.</summary>
    [ObservableProperty] public partial bool IsInitialized { get; set; }
    /// <summary>Constructs the state machine with injectable platform boundaries and timeout clock.</summary>
    public MainViewModel(ISettingsStore store, ISpeechSession speech, IRecordingWriter recording, Action<Action> dispatch, Func<TimeSpan, Task>? delayAsync = null)
    {
        _store = store; _speech = speech; _recording = recording; _dispatch = dispatch; _delayAsync = delayAsync ?? Task.Delay;
        speech.Status += message => _dispatch(() => Log(message));
        speech.Translated += translation => _dispatch(() => ReceiveTranslation(translation));
        speech.Ended += message => _dispatch(() => { Log(message); if (IsFrozen) { _isAcceptingResults = false; _ = StopAsync(); } });
    }
    /// <summary>All session activity retained until application exit.</summary>
    public ObservableCollection<string> Activity { get; } = [];
    /// <summary>All translated results retained until application exit.</summary>
    public ObservableCollection<Translation> Translations { get; } = [];
    /// <summary>Whether editing is safe.</summary>
    public bool CanEdit => IsInitialized && !IsFrozen && !IsStopPending && !IsPersisting && !HasRecoveryError;
    /// <summary>Whether a new session can start.</summary>
    public bool CanStart => CanEdit && _settings.HasCredentials;
    /// <summary>Whether stop or retry is available.</summary>
    public bool CanStop => IsFrozen && !IsStopPending;
    /// <summary>Whether the previous stop has not ended.</summary>
    public bool IsStopPending => _stopTask is { IsCompleted: false };
    /// <summary>Whether credentials differ from their committed values.</summary>
    public bool HasUnsavedChanges => Region != _settings.Region || Key != _settings.Key;
    /// <summary>Committed state, never including failed saves.</summary>
    public Settings CommittedSettings => _settings;
    partial void OnRegionChanged(string value) => OnPropertyChanged(nameof(HasUnsavedChanges));
    partial void OnKeyChanged(string value) => OnPropertyChanged(nameof(HasUnsavedChanges));
    partial void OnIsFrozenChanged(bool value) => Refresh();
    partial void OnIsPersistingChanged(bool value) => Refresh();
    partial void OnHasRecoveryErrorChanged(bool value) => Refresh();
    partial void OnIsInitializedChanged(bool value) => Refresh();
    private void Refresh()
    {
        OnPropertyChanged(nameof(CanEdit)); OnPropertyChanged(nameof(CanStart)); OnPropertyChanged(nameof(CanStop)); OnPropertyChanged(nameof(IsStopPending));
    }
    private void Log(string message) { Status = message; Activity.Add($"{DateTimeOffset.Now:HH:mm:ss} {message}"); }
    /// <summary>Loads persisted settings; environment fallback applies only after a successful empty load.</summary>
    public async Task InitializeAsync(Settings? environment = null)
    {
        if (IsInitialized) return;
        try
        {
            var saved = await _store.LoadAsync();
            _settings = saved ?? new();
            if (!_settings.HasCredentials && string.IsNullOrEmpty(_settings.Region) && string.IsNullOrEmpty(_settings.Key) && environment?.HasCredentials == true)
                _settings = _settings with { Region = environment.Region, Key = environment.Key };
            ApplyCommitted(); IsSettingsVisible = !_settings.HasCredentials;
        }
        catch (Exception)
        {
            HasRecoveryError = true; IsSettingsVisible = true;
            SettingsMessage = "設定の読み取りまたは復号に失敗しました。環境変数には切り替えません。元のデータを保持して明示的に復旧してください。";
        }
        finally { IsInitialized = true; }
    }
    private void ApplyCommitted()
    {
        Region = _settings.Region; Key = _settings.Key; Source = _settings.Source; Target = _settings.Target; Folder = _settings.Folder ?? "";
        OnPropertyChanged(nameof(HasUnsavedChanges)); OnPropertyChanged(nameof(CommittedSettings)); Refresh();
    }
    /// <summary>Discards only credential drafts, leaving committed preferences unchanged.</summary>
    public void CancelSettings() { Region = _settings.Region; Key = _settings.Key; SettingsMessage = "変更を破棄しました。"; }
    /// <summary>Commits validated credential drafts after durable save only.</summary>
    public async Task<bool> SaveSettingsAsync()
    {
        if (!CanEdit) return false;
        var next = _settings with { Region = Region.Trim(), Key = Key.Trim() };
        if (!next.HasCredentials) { SettingsMessage = "有効なリージョンと空白を含まない API キーを入力してください。"; return false; }
        IsPersisting = true;
        try { await _store.SaveAsync(next); _settings = next; ApplyCommitted(); SettingsMessage = "保存しました。キーは現在の Windows ユーザーで暗号化されています。"; return true; }
        catch { SettingsMessage = "保存に失敗しました。入力は保持されています。保存先を確認して再試行してください。"; return false; }
        finally { IsPersisting = false; }
    }
    /// <summary>Explicit confirmed recovery retaining originals. Caller must confirm before invoking.</summary>
    public async Task<bool> RecoverAsync()
    {
        if (!HasRecoveryError || IsFrozen || IsPersisting) return false;
        var next = _settings with { Region = Region.Trim(), Key = Key.Trim() };
        if (!next.HasCredentials) { SettingsMessage = "復旧するリージョンと API キーを入力してください。"; return false; }
        IsPersisting = true;
        try { var backup = await _store.RecoverAsync(next); _settings = next; HasRecoveryError = false; ApplyCommitted(); SettingsMessage = "復旧しました。元のデータ: " + backup; return true; }
        catch { SettingsMessage = "復旧に失敗しました。入力と退避済みの元データは保持されています。"; return false; }
        finally { IsPersisting = false; }
    }
    /// <summary>Immediately saves a selection, restoring displayed preferences on failure.</summary>
    public async Task<bool> SavePreferencesAsync(string source, string target, string? folder)
    {
        if (!CanEdit) { RestorePreferences(); return false; }
        if (source is not ("en-US" or "ja-JP") || target is not ("en-US" or "ja-JP")) { RestorePreferences(); return false; }
        if (folder is not null && (!Path.IsPathFullyQualified(folder) || !Directory.Exists(folder))) { SettingsMessage = "既存の絶対パスのフォルダーを選択してください。"; RestorePreferences(); return false; }
        var next = _settings with { Source = source, Target = target, Folder = folder };
        IsPersisting = true;
        try { await _store.SaveAsync(next); _settings = next; RestorePreferences(); OnPropertyChanged(nameof(CommittedSettings)); return true; }
        catch { RestorePreferences(); SettingsMessage = "選択の保存に失敗しました。以前の値に戻しました。"; return false; }
        finally { IsPersisting = false; }
    }
    private void RestorePreferences() { Source = _settings.Source; Target = _settings.Target; Folder = _settings.Folder ?? ""; }
    /// <summary>Starts at most one session and freezes edits before any asynchronous work.</summary>
    public Task StartAsync()
    {
        if (!CanStart) return Task.CompletedTask;
        try { _recordingPath = _recording.Validate(_settings.Folder, Filename); }
        catch (Exception ex) { Log("記録先を確認してください: " + ex.Message); return Task.CompletedTask; }
        IsFrozen = true; _isAcceptingResults = true; HasStopError = false;
        _startTask = StartCoreAsync();
        _ = CleanupFailedStartAsync(_startTask); return _startTask;
    }
    private async Task CleanupFailedStartAsync(Task start)
    {
        await start;
        if (!_isAcceptingResults && IsFrozen) await StopAsync();
    }
    private async Task StartCoreAsync()
    {
        try { Log("開始中…"); await _speech.StartAsync(_settings); if (_isAcceptingResults) Log("翻訳中"); }
        catch { _isAcceptingResults = false; Log("開始に失敗しました。停止してリソースを解放します。" ); }
    }
    /// <summary>Returns the same pending stop; completed failures permit a fresh retry.</summary>
    public Task StopAsync()
    {
        if (IsStopPending) return _stopTask!;
        if (!IsFrozen) return Task.CompletedTask;
        _isAcceptingResults = false;
        // Yield prevents a synchronous SDK event or completion from reentering before the task is assigned.
        _stopTask = StopCoreAsync(); Refresh();
        _ = RefreshAfterStopAsync(_stopTask); return _stopTask;
    }
    private async Task RefreshAfterStopAsync(Task stop)
    {
        await stop;
        _dispatch(Refresh);
    }
    private async Task StopCoreAsync()
    {
        await Task.Yield();
        _isAcceptingResults = false; HasStopError = false; Log("停止中…");
        try
        {
            if (_startTask is not null) await _startTask;
            var stop = _speech.StopAsync();
            if (await Task.WhenAny(stop, _delayAsync(TimeSpan.FromSeconds(10))) != stop)
                Log("停止に10秒以上かかっています。完了を待っています。編集は無効のままです。" );
            await stop; _recordingPath = null; IsFrozen = false; Log("停止");
        }
        catch { HasStopError = true; Log("停止に失敗しました。編集は無効です。停止を再試行してください。" ); }
        finally { _dispatch(Refresh); }
    }
    private void ReceiveTranslation(Translation translation)
    {
        if (!_isAcceptingResults) return;
        Translations.Add(translation); Log("原文: " + translation.SourceText); Log("翻訳: " + translation.TranslatedText);
        if (_recordingPath is null) return;
        try { _recording.Append(_recordingPath, translation); }
        catch { _isAcceptingResults = false; Log("記録ファイルの書き込みに失敗しました。翻訳を停止します。" ); _ = StopAsync(); }
    }
}
