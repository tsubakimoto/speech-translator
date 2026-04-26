using SpeechTranslatorDesktop.Commands;
using SpeechTranslatorDesktop.Models;
using SpeechTranslatorDesktop.Services;

namespace SpeechTranslatorDesktop.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly IUiDispatcher _dispatcher;
    private readonly ISpeechCredentialsProvider _credentialsProvider;
    private readonly IAzureAiServiceSettingsStore _settingsStore;
    private readonly IRecordingFileService _recordingFileService;
    private readonly ITranslationController _translationController;
    private readonly IDesktopTranslationWorkerFactory _workerFactory;
    private IDesktopTranslationWorker? _currentWorker;
    private LanguageOption? _selectedSourceLanguage;
    private LanguageOption? _selectedTargetLanguage;
    private string _azureApiKey = string.Empty;
    private string _azureRegion = string.Empty;
    private string _recordingFileName = string.Empty;
    private string _settingsStatusMessage = string.Empty;
    private string _statusMessage = "停止";
    private bool _isRunning;

    public MainViewModel(
        IUiDispatcher dispatcher,
        ISpeechCredentialsProvider credentialsProvider,
        IAzureAiServiceSettingsStore settingsStore,
        IRecordingFileService recordingFileService,
        ITranslationController translationController,
        IDesktopTranslationWorkerFactory workerFactory)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _credentialsProvider = credentialsProvider ?? throw new ArgumentNullException(nameof(credentialsProvider));
        _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        _recordingFileService = recordingFileService ?? throw new ArgumentNullException(nameof(recordingFileService));
        _translationController = translationController ?? throw new ArgumentNullException(nameof(translationController));
        _workerFactory = workerFactory ?? throw new ArgumentNullException(nameof(workerFactory));

        AvailableLanguages =
        [
            new LanguageOption("en-US", "English (en-US)"),
            new LanguageOption("ja-JP", "Japanese (ja-JP)")
        ];

        _selectedSourceLanguage = AvailableLanguages[0];
        _selectedTargetLanguage = AvailableLanguages[1];

        StartCommand = new AsyncRelayCommand(StartAsync, () => !IsRunning, _dispatcher, HandleCommandException);
        StopCommand = new AsyncRelayCommand(StopAsync, () => IsRunning, _dispatcher, HandleCommandException);
        SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync, dispatcher: _dispatcher, onException: HandleSettingsCommandException);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<string> ActivityLogs { get; } = [];

    public ObservableCollection<LanguageOption> AvailableLanguages { get; }

    public string AzureApiKey
    {
        get => _azureApiKey;
        set
        {
            if (_azureApiKey == value)
            {
                return;
            }

            _azureApiKey = value;
            OnPropertyChanged();
        }
    }

    public string AzureRegion
    {
        get => _azureRegion;
        set
        {
            if (_azureRegion == value)
            {
                return;
            }

            _azureRegion = value;
            OnPropertyChanged();
        }
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (_isRunning == value)
            {
                return;
            }

            _isRunning = value;
            OnPropertyChanged();
            StartCommand.RaiseCanExecuteChanged();
            StopCommand.RaiseCanExecuteChanged();
        }
    }

    public string RecordingFileName
    {
        get => _recordingFileName;
        set
        {
            if (_recordingFileName == value)
            {
                return;
            }

            _recordingFileName = value;
            OnPropertyChanged();
        }
    }

    public LanguageOption? SelectedSourceLanguage
    {
        get => _selectedSourceLanguage;
        set
        {
            if (_selectedSourceLanguage == value)
            {
                return;
            }

            _selectedSourceLanguage = value;
            OnPropertyChanged();
        }
    }

    public LanguageOption? SelectedTargetLanguage
    {
        get => _selectedTargetLanguage;
        set
        {
            if (_selectedTargetLanguage == value)
            {
                return;
            }

            _selectedTargetLanguage = value;
            OnPropertyChanged();
        }
    }

    public AsyncRelayCommand StartCommand { get; }

    public AsyncRelayCommand SaveSettingsCommand { get; }

    public string SettingsStatusMessage
    {
        get => _settingsStatusMessage;
        private set
        {
            if (_settingsStatusMessage == value)
            {
                return;
            }

            _settingsStatusMessage = value;
            OnPropertyChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (_statusMessage == value)
            {
                return;
            }

            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public AsyncRelayCommand StopCommand { get; }

    public ObservableCollection<TranslationLogItem> TranslationLogs { get; } = [];

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var savedSettings = await _settingsStore.LoadAsync(cancellationToken);
            if (savedSettings is null)
            {
                SettingsStatusMessage = "Azure AI Service のリージョンと API キーを入力して保存してください。未保存の場合は SPEECH_REGION / SPEECH_KEY をフォールバックとして使用します。";
                return;
            }

            AzureRegion = savedSettings.Region;
            AzureApiKey = savedSettings.ApiKey;
            SettingsStatusMessage = "保存済みの Azure AI Service 設定を読み込みました。";
        }
        catch (Exception ex)
        {
            SettingsStatusMessage = $"設定の読み込みに失敗しました: {ex.Message}";
            AddActivityLog(SettingsStatusMessage);
        }
    }

    private async Task StartAsync()
    {
        if (SelectedSourceLanguage is null || SelectedTargetLanguage is null)
        {
            StatusMessage = "言語を選択してください。";
            return;
        }

        string? recordingFileName;
        try
        {
            recordingFileName = _recordingFileService.NormalizeFileName(RecordingFileName);
            RecordingFileName = recordingFileName ?? string.Empty;
        }
        catch (ArgumentException ex)
        {
            StatusMessage = $"記録ファイル名が不正です: {ex.Message}";
            AddActivityLog(StatusMessage);
            return;
        }

        var credentialsResult = _credentialsProvider.GetCredentials(AzureRegion, AzureApiKey);
        if (!credentialsResult.IsValid || credentialsResult.Credentials is null)
        {
            StatusMessage = credentialsResult.ErrorMessage;
            AddActivityLog(credentialsResult.ErrorMessage);
            return;
        }

        var worker = _workerFactory.Create(SelectedTargetLanguage.Code, recordingFileName);
        SubscribeWorker(worker);

        try
        {
            await _translationController.StartAsync(
                credentialsResult.Credentials,
                SelectedSourceLanguage.Code,
                SelectedTargetLanguage.Code,
                worker.RecognizerWorker);

            _currentWorker = worker;
            IsRunning = true;
            StatusMessage = "開始";
            AddActivityLog("翻訳を開始しました。");
        }
        catch (Exception ex)
        {
            UnsubscribeWorker(worker);
            StatusMessage = ex.Message;
            AddActivityLog(ex.Message);
        }
    }

    private async Task SaveSettingsAsync()
    {
        var normalizedRegion = AzureRegion.Trim();
        var normalizedApiKey = AzureApiKey.Trim();

        if (string.IsNullOrWhiteSpace(normalizedRegion) || string.IsNullOrWhiteSpace(normalizedApiKey))
        {
            SettingsStatusMessage = "Azure AI Service のリージョンと API キーを入力してから保存してください。";
            return;
        }

        await _settingsStore.SaveAsync(new AzureAiServiceSettings(normalizedRegion, normalizedApiKey));

        AzureRegion = normalizedRegion;
        AzureApiKey = normalizedApiKey;
        SettingsStatusMessage = "Azure AI Service 設定を保存しました。";
        AddActivityLog("Azure AI Service 設定を保存しました。");
    }

    private async Task StopAsync()
    {
        try
        {
            await _translationController.StopAsync();
            StatusMessage = "停止";
            AddActivityLog("翻訳を停止しました。");
        }
        catch (Exception ex)
        {
            StatusMessage = $"停止に失敗しました: {ex.Message}";
            AddActivityLog(StatusMessage);
        }
        finally
        {
            IsRunning = _translationController.IsRunning;

            if (!IsRunning)
            {
                DetachCurrentWorker();
            }
        }
    }

    private void SubscribeWorker(IDesktopTranslationWorker worker)
    {
        worker.StatusChanged += OnWorkerStatusChanged;
        worker.MessageLogged += OnWorkerMessageLogged;
        worker.TranslationLogged += OnWorkerTranslationLogged;
    }

    private void UnsubscribeWorker(IDesktopTranslationWorker worker)
    {
        worker.StatusChanged -= OnWorkerStatusChanged;
        worker.MessageLogged -= OnWorkerMessageLogged;
        worker.TranslationLogged -= OnWorkerTranslationLogged;
    }

    private void OnWorkerStatusChanged(object? sender, WorkerStatusChangedEventArgs e)
    {
        _dispatcher.Invoke(() =>
        {
            StatusMessage = e.Message;
            if (e.Status != DesktopTranslationStatus.Recognizing)
            {
                AddActivityLog(e.Message);
            }

            if (e.Status is DesktopTranslationStatus.Canceled or DesktopTranslationStatus.SessionStopped)
            {
                IsRunning = false;

                if (sender is IDesktopTranslationWorker worker)
                {
                    UnsubscribeWorker(worker);

                    if (ReferenceEquals(_currentWorker, worker))
                    {
                        _currentWorker = null;
                    }
                }
            }
        });
    }

    private void OnWorkerMessageLogged(object? sender, string e)
    {
        _dispatcher.Invoke(() => AddActivityLog(e));
    }

    private void OnWorkerTranslationLogged(object? sender, TranslationLogItem e)
    {
        _dispatcher.Invoke(() => TranslationLogs.Add(e));
    }

    private void AddActivityLog(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        ActivityLogs.Add(message);
    }

    private void DetachCurrentWorker()
    {
        if (_currentWorker is null)
        {
            return;
        }

        UnsubscribeWorker(_currentWorker);
        _currentWorker = null;
    }

    private void HandleCommandException(Exception ex)
    {
        _dispatcher.Invoke(() =>
        {
            StatusMessage = ex.Message;
            AddActivityLog(ex.Message);
            IsRunning = _translationController.IsRunning;

            if (!IsRunning)
            {
                DetachCurrentWorker();
            }
        });
    }

    private void HandleSettingsCommandException(Exception ex)
    {
        _dispatcher.Invoke(() =>
        {
            SettingsStatusMessage = $"設定の保存に失敗しました: {ex.Message}";
            AddActivityLog(SettingsStatusMessage);
        });
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
