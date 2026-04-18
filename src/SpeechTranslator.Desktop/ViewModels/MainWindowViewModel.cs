using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SpeechTranslator.Desktop;

public sealed class MainWindowViewModel : ObservableObject, ITranslationSessionViewModel
{
    private readonly IUiDispatcher _dispatcher;
    private readonly IDesktopSettingsStore _settingsStore;
    private readonly IRecordingPathService _recordingPathService;
    private readonly IMicrophoneDeviceService _microphoneDeviceService;
    private readonly IRecordingDirectoryPicker _recordingDirectoryPicker;
    private readonly IRecordingWriterFactory _recordingWriterFactory;
    private readonly ITranslatorService _translatorService;
    private readonly RelayCommand _startCommand;
    private readonly AsyncRelayCommand _stopCommand;
    private readonly RelayCommand _selectRecordingDirectoryCommand;
    private IRecordingWriter? _recordingWriter;
    private Task? _sessionMonitorTask;
    private string _region = string.Empty;
    private string _subscriptionKey = string.Empty;
    private string _recordingName = string.Empty;
    private string _recordingDirectory = string.Empty;
    private string _currentSourceText = string.Empty;
    private string _currentTranslatedText = string.Empty;
    private string _statusMessage = "Ready.";
    private bool _isRunning;
    private bool _userInitiatedStopRequested;
    private LanguageOption? _selectedSourceLanguage;
    private LanguageOption? _selectedTargetLanguage;
    private MicrophoneDeviceOption? _selectedMicrophoneDevice;

    public MainWindowViewModel(
        IUiDispatcher dispatcher,
        IDesktopSettingsStore settingsStore,
        IRecordingPathService recordingPathService,
        ITranslatorService translatorService,
        IRecordingWriterFactory? recordingWriterFactory = null,
        IMicrophoneDeviceService? microphoneDeviceService = null,
        IRecordingDirectoryPicker? recordingDirectoryPicker = null)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        _recordingPathService = recordingPathService ?? throw new ArgumentNullException(nameof(recordingPathService));
        _translatorService = translatorService ?? throw new ArgumentNullException(nameof(translatorService));
        _recordingWriterFactory = recordingWriterFactory ?? new RecordingWriterFactory();
        _microphoneDeviceService = microphoneDeviceService ?? new MicrophoneDeviceService();
        _recordingDirectoryPicker = recordingDirectoryPicker ?? new RecordingDirectoryPickerService();

        Languages = [
            new("en-US", "English (en-US)"),
            new("ja-JP", "Japanese (ja-JP)")
        ];

        MicrophoneDevices = BuildMicrophoneDevices(_microphoneDeviceService.GetInputDevices());

        var settings = _settingsStore.Load()?.Normalize() ?? DesktopSettings.CreateDefault();
        _region = settings.Region;
        _recordingName = settings.RecordingName;
        _recordingDirectory = string.IsNullOrWhiteSpace(settings.RecordingDirectory)
            ? (_recordingPathService as RecordingPathService)?.GetDefaultRecordingDirectory() ?? string.Empty
            : settings.RecordingDirectory;
        _selectedSourceLanguage = Languages.FirstOrDefault(language => language.Code == settings.SourceLanguage) ?? Languages[0];
        _selectedTargetLanguage = Languages.FirstOrDefault(language => language.Code == settings.TargetLanguage) ?? Languages[1];
        _selectedMicrophoneDevice = MicrophoneDevices.FirstOrDefault(device => device.DeviceName == settings.MicrophoneDeviceName) ?? MicrophoneDevices[0];

        _startCommand = new RelayCommand(() => _ = StartAsync(), CanStart);
        _stopCommand = new AsyncRelayCommand(StopAsync, () => IsRunning);
        _selectRecordingDirectoryCommand = new RelayCommand(SelectRecordingDirectory);
    }

    public IReadOnlyList<LanguageOption> Languages { get; }

    public IReadOnlyList<MicrophoneDeviceOption> MicrophoneDevices { get; }

    public ObservableCollection<TranslationEntry> Translations { get; } = [];

    public ICommand StartCommand => _startCommand;

    public ICommand StopCommand => _stopCommand;

    public ICommand SelectRecordingDirectoryCommand => _selectRecordingDirectoryCommand;

    public string Region
    {
        get => _region;
        set
        {
            if (SetProperty(ref _region, value))
            {
                NotifyCommands();
            }
        }
    }

    public string SubscriptionKey
    {
        get => _subscriptionKey;
        set
        {
            if (SetProperty(ref _subscriptionKey, value))
            {
                NotifyCommands();
            }
        }
    }

    public string RecordingName
    {
        get => _recordingName;
        set
        {
            if (SetProperty(ref _recordingName, value))
            {
                NotifyCommands();
            }
        }
    }

    public string RecordingDirectory
    {
        get => _recordingDirectory;
        set
        {
            if (SetProperty(ref _recordingDirectory, value))
            {
                NotifyCommands();
            }
        }
    }

    public LanguageOption? SelectedSourceLanguage
    {
        get => _selectedSourceLanguage;
        set
        {
            if (SetProperty(ref _selectedSourceLanguage, value))
            {
                NotifyCommands();
            }
        }
    }

    public LanguageOption? SelectedTargetLanguage
    {
        get => _selectedTargetLanguage;
        set
        {
            if (SetProperty(ref _selectedTargetLanguage, value))
            {
                NotifyCommands();
            }
        }
    }

    public MicrophoneDeviceOption? SelectedMicrophoneDevice
    {
        get => _selectedMicrophoneDevice;
        set
        {
            if (SetProperty(ref _selectedMicrophoneDevice, value))
            {
                NotifyCommands();
            }
        }
    }

    public string CurrentSourceText
    {
        get => _currentSourceText;
        private set => SetProperty(ref _currentSourceText, value);
    }

    public string CurrentTranslatedText
    {
        get => _currentTranslatedText;
        private set => SetProperty(ref _currentTranslatedText, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetProperty(ref _isRunning, value))
            {
                NotifyCommands();
            }
        }
    }

    public Task StartAsync()
    {
        if (IsRunning)
        {
            return Task.CompletedTask;
        }

        if (!TryBuildSessionOptions(out var sessionOptions))
        {
            return Task.CompletedTask;
        }

        string? preferenceSaveMessage = null;
        if (!TrySavePreferences())
        {
            preferenceSaveMessage = StatusMessage;
        }

        _recordingWriter?.Dispose();
        _recordingWriter = null;

        string? recordingPath;
        try
        {
            recordingPath = _recordingPathService.GetRecordingFilePath(RecordingDirectory, RecordingName);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException or ArgumentException or NotSupportedException)
        {
            StatusMessage = $"Unable to create recording file: {ex.Message}";
            return Task.CompletedTask;
        }

        if (recordingPath is not null)
        {
            try
            {
                _recordingWriter = _recordingWriterFactory.Create(recordingPath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException or ArgumentException or NotSupportedException)
            {
                StatusMessage = $"Unable to create recording file: {ex.Message}";
                return Task.CompletedTask;
            }
        }
        else
        {
            StatusMessage = "Recording name is invalid.";
            return Task.CompletedTask;
        }

        StatusMessage = preferenceSaveMessage ?? "Starting translation...";
        try
        {
            _userInitiatedStopRequested = false;
            var worker = new DesktopTranslationRecognizerWorker(_dispatcher, this, _recordingWriter, SelectedTargetLanguage!.Code);
            var sessionTask = _translatorService.StartAsync(sessionOptions, worker);
            IsRunning = true;
            _sessionMonitorTask = MonitorSessionAsync(sessionTask);
        }
        catch (Exception ex)
        {
            _recordingWriter?.Dispose();
            _recordingWriter = null;
            StatusMessage = ex.Message;
            IsRunning = false;
        }

        return Task.CompletedTask;
    }

    public void SavePreferences()
    {
        _settingsStore.Save(CreateSettingsSnapshot());
    }

    public bool TrySavePreferences()
    {
        try
        {
            SavePreferences();
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException or ArgumentException or NotSupportedException)
        {
            StatusMessage = $"Failed to save preferences: {ex.Message}";
            return false;
        }
    }

    public async Task StopAsync()
    {
        if (!IsRunning)
        {
            return;
        }

        _userInitiatedStopRequested = true;
        StatusMessage = "Stopping translation...";
        try
        {
            await _translatorService.StopAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    public void SetCurrentSourceText(string text) => CurrentSourceText = text;

    public void SetCurrentTranslatedText(string text) => CurrentTranslatedText = text;

    public void AddTranslation(TranslationEntry entry) => Translations.Add(entry);

    public void SetStatusMessage(string text) => StatusMessage = text;

    private void SelectRecordingDirectory()
    {
        var selectedDirectory = _recordingDirectoryPicker.PickDirectory(RecordingDirectory);
        if (!string.IsNullOrWhiteSpace(selectedDirectory))
        {
            RecordingDirectory = selectedDirectory;
        }
    }

    private bool TryBuildSessionOptions(out TranslationSessionOptions options)
    {
        options = default!;

        if (string.IsNullOrWhiteSpace(Region))
        {
            StatusMessage = "Azure Region is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(SubscriptionKey))
        {
            StatusMessage = "Subscription Key is required.";
            return false;
        }

        if (SelectedSourceLanguage is null || SelectedTargetLanguage is null)
        {
            StatusMessage = "Select source and target languages.";
            return false;
        }

        options = new TranslationSessionOptions(
            Region.Trim(),
            SubscriptionKey.Trim(),
            SelectedSourceLanguage.Code,
            SelectedTargetLanguage.Code,
            SelectedMicrophoneDevice?.DeviceName ?? string.Empty);
        return true;
    }

    private async Task MonitorSessionAsync(Task sessionTask)
    {
        string? message = null;
        try
        {
            await sessionTask.ConfigureAwait(false);
            if (_userInitiatedStopRequested)
            {
                message = "Translation stopped.";
            }
        }
        catch (OperationCanceledException)
        {
            if (_userInitiatedStopRequested)
            {
                message = "Translation stopped.";
            }
        }
        catch (Exception ex)
        {
            message = ex.Message;
        }
        finally
        {
            var writer = Interlocked.Exchange(ref _recordingWriter, null);
            writer?.Dispose();
            _sessionMonitorTask = null;
            _userInitiatedStopRequested = false;
        }

        _dispatcher.Post(() =>
        {
            if (message is not null)
            {
                StatusMessage = message;
            }

            IsRunning = false;
        });
    }

    private void NotifyCommands()
    {
        _startCommand.NotifyCanExecuteChanged();
        _stopCommand.NotifyCanExecuteChanged();
    }

    private bool CanStart() => !IsRunning && !string.IsNullOrWhiteSpace(Region) && !string.IsNullOrWhiteSpace(SubscriptionKey) && SelectedSourceLanguage is not null && SelectedTargetLanguage is not null;

    private DesktopSettings CreateSettingsSnapshot()
    {
        return new DesktopSettings(
            Region?.Trim() ?? string.Empty,
            SelectedSourceLanguage?.Code ?? Languages[0].Code,
            SelectedTargetLanguage?.Code ?? Languages[1].Code,
            RecordingName?.Trim() ?? string.Empty,
            RecordingDirectory?.Trim() ?? string.Empty,
            SelectedMicrophoneDevice?.DeviceName ?? string.Empty);
    }

    private static IReadOnlyList<MicrophoneDeviceOption> BuildMicrophoneDevices(IReadOnlyList<MicrophoneDeviceOption> devices)
    {
        var microphoneDevices = new List<MicrophoneDeviceOption>();
        if (devices.All(device => !string.IsNullOrWhiteSpace(device.DeviceName)))
        {
            microphoneDevices.Add(new MicrophoneDeviceOption(string.Empty, "Default microphone"));
        }

        foreach (var device in devices)
        {
            if (string.IsNullOrWhiteSpace(device.DeviceName))
            {
                if (microphoneDevices.All(existing => !string.IsNullOrWhiteSpace(existing.DeviceName)))
                {
                    microphoneDevices.Add(new MicrophoneDeviceOption(string.Empty, "Default microphone"));
                }
            }
            else if (microphoneDevices.All(existing => !string.Equals(existing.DeviceName, device.DeviceName, StringComparison.OrdinalIgnoreCase)))
            {
                microphoneDevices.Add(device);
            }
        }

        if (microphoneDevices.Count == 0)
        {
            microphoneDevices.Add(new MicrophoneDeviceOption(string.Empty, "Default microphone"));
        }

        return microphoneDevices;
    }
}
