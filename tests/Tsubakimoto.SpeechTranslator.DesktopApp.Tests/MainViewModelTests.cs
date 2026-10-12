using System.Windows.Input;

using Tsubakimoto.SpeechTranslator.DesktopApp.Commands;
using Tsubakimoto.SpeechTranslator.DesktopApp.Models;
using Tsubakimoto.SpeechTranslator.DesktopApp.Services;
using Tsubakimoto.SpeechTranslator.DesktopApp.ViewModels;
using Tsubakimoto.SpeechTranslator.Shared;

namespace Tsubakimoto.SpeechTranslator.DesktopApp.Tests;

public class MainViewModelTests
{
    [Fact]
    public void InitialState_StartEnabled_StopDisabled()
    {
        // Arrange
        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.True(viewModel.StartCommand.CanExecute(null));
        Assert.False(viewModel.StopCommand.CanExecute(null));
    }

    [Fact]
    public async Task Start_WhenCredentialsMissing_ShowsErrorAndDoesNotStartAsync()
    {
        // Arrange
        var translationController = new FakeTranslationController();
        var viewModel = CreateViewModel(
            credentialsProvider: new FakeSpeechCredentialsProvider(SpeechCredentialsResult.Failure("Missing SPEECH_REGION and SPEECH_KEY.")),
            translationController: translationController);

        // Act
        await ExecuteAsync(viewModel.StartCommand);

        // Assert
        Assert.Equal(0, translationController.StartCallCount);
        Assert.Contains("SPEECH_REGION", viewModel.StatusMessage);
        Assert.Contains("SPEECH_KEY", viewModel.StatusMessage);
        Assert.False(viewModel.IsRunning);
    }

    [Fact]
    public async Task InitializeAsync_WhenSavedSettingsExist_LoadsThemIntoViewModelAsync()
    {
        // Arrange
        var viewModel = CreateViewModel(
            settingsStore: new FakeAzureAiServiceSettingsStore
            {
                LoadedSettings = new AzureAiServiceSettings("japaneast", "saved-key")
            });

        // Act
        await viewModel.InitializeAsync();

        // Assert
        Assert.Equal("japaneast", viewModel.AzureRegion);
        Assert.Equal("saved-key", viewModel.AzureApiKey);
        Assert.Contains("読み込み", viewModel.SettingsStatusMessage);
    }

    [Fact]
    public async Task InitializeAsync_WhenSavedSettingsDoNotExist_ShowsGuidanceAsync()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        await viewModel.InitializeAsync();

        // Assert
        Assert.Empty(viewModel.AzureRegion);
        Assert.Empty(viewModel.AzureApiKey);
        Assert.Contains("保存", viewModel.SettingsStatusMessage);
        Assert.Contains("SPEECH_REGION", viewModel.SettingsStatusMessage);
    }

    [Fact]
    public async Task InitializeAsync_OnFirstLaunchWithMissingSettingsDatabase_ShowsGuidanceInsteadOfFailureAsync()
    {
        // Arrange
        var testDirectory = Path.Combine(
            AppContext.BaseDirectory,
            "test-artifacts",
            nameof(MainViewModelTests),
            Guid.NewGuid().ToString("N"));

        try
        {
            var databasePath = Path.Combine(testDirectory, "nested", "speech-translator-desktop.db");
            var viewModel = CreateViewModel(
                settingsStore: new SqliteAzureAiServiceSettingsStore(databasePath, new FakeSecretProtector()));

            // Act
            await viewModel.InitializeAsync();

            // Assert
            Assert.Contains("保存", viewModel.SettingsStatusMessage);
            Assert.Contains("SPEECH_REGION", viewModel.SettingsStatusMessage);
            Assert.DoesNotContain("失敗", viewModel.SettingsStatusMessage);
        }
        finally
        {
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task SaveSettings_WhenValuesAreValid_PersistsThemAsync()
    {
        // Arrange
        var settingsStore = new FakeAzureAiServiceSettingsStore();
        var viewModel = CreateViewModel(settingsStore: settingsStore);
        viewModel.AzureRegion = "japaneast";
        viewModel.AzureApiKey = "saved-key";

        // Act
        await ExecuteAsync(viewModel.SaveSettingsCommand);

        // Assert
        Assert.Equal(1, settingsStore.SaveCallCount);
        Assert.NotNull(settingsStore.SavedSettings);
        Assert.Equal("japaneast", settingsStore.SavedSettings.Region);
        Assert.Equal("saved-key", settingsStore.SavedSettings.ApiKey);
        Assert.Contains("保存", viewModel.SettingsStatusMessage);
    }

    [Fact]
    public async Task SaveSettings_WhenValuesAreMissing_ShowsValidationErrorAsync()
    {
        // Arrange
        var settingsStore = new FakeAzureAiServiceSettingsStore();
        var viewModel = CreateViewModel(settingsStore: settingsStore);
        viewModel.AzureRegion = "japaneast";
        viewModel.AzureApiKey = "";

        // Act
        await ExecuteAsync(viewModel.SaveSettingsCommand);

        // Assert
        Assert.Equal(0, settingsStore.SaveCallCount);
        Assert.Contains("API キー", viewModel.SettingsStatusMessage);
    }

    [Fact]
    public async Task Start_WhenCredentialsPresent_StartsTranslationAsync()
    {
        // Arrange
        var translationController = new FakeTranslationController();
        var viewModel = CreateViewModel(translationController: translationController);

        // Act
        await ExecuteAsync(viewModel.StartCommand);

        // Assert
        Assert.Equal(1, translationController.StartCallCount);
        Assert.Equal("開始", viewModel.StatusMessage);
        Assert.True(viewModel.IsRunning);
        Assert.False(viewModel.StartCommand.CanExecute(null));
        Assert.True(viewModel.StopCommand.CanExecute(null));
    }

    [Fact]
    public async Task Start_WhenConfiguredCredentialsPresent_PassesThemToControllerAsync()
    {
        // Arrange
        var translationController = new FakeTranslationController();
        var viewModel = CreateViewModel(
            credentialsProvider: new FakeSpeechCredentialsProvider(SpeechCredentialsResult.Success(new SpeechCredentials("ui-region", "ui-key"))),
            translationController: translationController);
        viewModel.AzureRegion = "ui-region";
        viewModel.AzureApiKey = "ui-key";

        // Act
        await ExecuteAsync(viewModel.StartCommand);

        // Assert
        Assert.NotNull(translationController.LastStartCredentials);
        Assert.Equal("ui-region", translationController.LastStartCredentials.Region);
        Assert.Equal("ui-key", translationController.LastStartCredentials.Key);
    }

    [Fact]
    public async Task Start_WhenRecordingFileNameIsInvalid_ShowsErrorAndDoesNotStartAsync()
    {
        // Arrange
        var translationController = new FakeTranslationController();
        var workerFactory = new FakeDesktopTranslationWorkerFactory(new FakeDesktopTranslationWorker());
        var viewModel = CreateViewModel(
            recordingFileService: new FakeRecordingFileService
            {
                NormalizeFileNameException = new ArgumentException("ファイル名には英数字、ハイフン、アンダースコアのみ使用できます。", "fileName")
            },
            translationController: translationController,
            workerFactory: workerFactory);
        viewModel.RecordingFileName = "bad name";

        // Act
        await ExecuteAsync(viewModel.StartCommand);

        // Assert
        Assert.Equal(0, translationController.StartCallCount);
        Assert.Equal(0, workerFactory.CreateCallCount);
        Assert.False(viewModel.IsRunning);
        Assert.StartsWith("記録ファイル名が不正です:", viewModel.StatusMessage);
        Assert.Contains(viewModel.StatusMessage, viewModel.ActivityLogs);
    }

    [Fact]
    public async Task Stop_CallsTranslationControllerAsync()
    {
        // Arrange
        var translationController = new FakeTranslationController();
        var viewModel = CreateViewModel(translationController: translationController);

        // Act
        await ExecuteAsync(viewModel.StartCommand);
        await ExecuteAsync(viewModel.StopCommand);

        // Assert
        Assert.Equal(1, translationController.StopCallCount);
        Assert.Equal("停止", viewModel.StatusMessage);
        Assert.False(viewModel.IsRunning);
    }

    [Fact]
    public async Task Stop_WhenControllerThrows_ShowsErrorAndKeepsRunningAsync()
    {
        // Arrange
        var translationController = new FakeTranslationController
        {
            StopException = new InvalidOperationException("stop failed"),
            KeepRunningOnStopFailure = true
        };
        var viewModel = CreateViewModel(translationController: translationController);

        // Act
        await ExecuteAsync(viewModel.StartCommand);
        await ExecuteAsync(viewModel.StopCommand);

        // Assert
        Assert.Equal("停止に失敗しました: stop failed", viewModel.StatusMessage);
        Assert.Contains("停止に失敗しました: stop failed", viewModel.ActivityLogs);
        Assert.True(viewModel.IsRunning);
        Assert.False(viewModel.StartCommand.CanExecute(null));
        Assert.True(viewModel.StopCommand.CanExecute(null));
    }

    [Fact]
    public async Task WorkerTranslationEvent_AddsTranslationLogAsync()
    {
        // Arrange
        var worker = new FakeDesktopTranslationWorker();
        var viewModel = CreateViewModel(workerFactory: new FakeDesktopTranslationWorkerFactory(worker));

        // Act
        await ExecuteAsync(viewModel.StartCommand);
        worker.RaiseTranslationLogged(new TranslationLogItem("hello", "こんにちは"));

        // Assert
        Assert.Single(viewModel.TranslationLogs);
        Assert.Equal("hello", viewModel.TranslationLogs[0].SourceText);
        Assert.Equal("こんにちは", viewModel.TranslationLogs[0].TranslatedText);
    }

    [Fact]
    public async Task WorkerStatusEvents_UpdateStatusAndLogsAsync()
    {
        // Arrange
        var worker = new FakeDesktopTranslationWorker();
        var viewModel = CreateViewModel(workerFactory: new FakeDesktopTranslationWorkerFactory(worker));

        // Act
        await ExecuteAsync(viewModel.StartCommand);
        worker.RaiseStatusChanged(DesktopTranslationStatus.NoMatch, "NoMatch");
        worker.RaiseMessageLogged("Session stopped.");
        worker.RaiseStatusChanged(DesktopTranslationStatus.SessionStopped, "セッション停止");

        // Assert
        Assert.Equal("セッション停止", viewModel.StatusMessage);
        Assert.Contains("Session stopped.", viewModel.ActivityLogs);
        Assert.False(viewModel.IsRunning);
    }

    [Fact]
    public async Task Start_WhenControllerCompletesAsynchronously_RaisesPropertyChangesOnCapturedContextAsync()
    {
        await RunOnSynchronizationContextAsync(async uiThreadId =>
        {
            // Arrange
            var translationController = new FakeTranslationController { StartShouldYield = true };
            var viewModel = CreateViewModel(
                dispatcher: new SynchronizationContextDispatcher(SynchronizationContext.Current!),
                translationController: translationController);
            var propertyChangedThreadIds = new List<int>();
            viewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(MainViewModel.IsRunning) or nameof(MainViewModel.StatusMessage))
                {
                    propertyChangedThreadIds.Add(Environment.CurrentManagedThreadId);
                }
            };

            // Act
            await ExecuteAsync(viewModel.StartCommand);

            // Assert
            Assert.NotEmpty(propertyChangedThreadIds);
            Assert.All(propertyChangedThreadIds, threadId => Assert.Equal(uiThreadId, threadId));
        });
    }

    [Fact]
    public async Task WorkerTranslationEvent_FromBackgroundThread_UpdatesCollectionOnUiThreadAsync()
    {
        await RunOnSynchronizationContextAsync(async uiThreadId =>
        {
            // Arrange
            var worker = new FakeDesktopTranslationWorker();
            var viewModel = CreateViewModel(
                dispatcher: new SynchronizationContextDispatcher(SynchronizationContext.Current!),
                workerFactory: new FakeDesktopTranslationWorkerFactory(worker));
            int? collectionChangedThreadId = null;

            viewModel.TranslationLogs.CollectionChanged += (_, _) => collectionChangedThreadId = Environment.CurrentManagedThreadId;

            // Act
            await ExecuteAsync(viewModel.StartCommand);
            await Task.Run(() => worker.RaiseTranslationLogged(new TranslationLogItem("hello", "こんにちは")));

            // Assert
            Assert.Equal(uiThreadId, collectionChangedThreadId);
            Assert.Single(viewModel.TranslationLogs);
        });
    }

    private static MainViewModel CreateViewModel(
        IUiDispatcher? dispatcher = null,
        ISpeechCredentialsProvider? credentialsProvider = null,
        IAzureAiServiceSettingsStore? settingsStore = null,
        IRecordingFileService? recordingFileService = null,
        ITranslationController? translationController = null,
        IDesktopTranslationWorkerFactory? workerFactory = null)
    {
        return new MainViewModel(
            dispatcher ?? new ImmediateDispatcher(),
            credentialsProvider ?? new FakeSpeechCredentialsProvider(SpeechCredentialsResult.Success(new SpeechCredentials("japaneast", "test-key"))),
            settingsStore ?? new FakeAzureAiServiceSettingsStore(),
            recordingFileService ?? new FakeRecordingFileService(),
            translationController ?? new FakeTranslationController(),
            workerFactory ?? new FakeDesktopTranslationWorkerFactory(new FakeDesktopTranslationWorker()));
    }

    private static Task ExecuteAsync(ICommand command)
    {
        return ((AsyncRelayCommand)command).ExecuteAsync(null);
    }

    private sealed class ImmediateDispatcher : IUiDispatcher
    {
        public void Invoke(Action action) => action();
    }

    private sealed class FakeSpeechCredentialsProvider : ISpeechCredentialsProvider
    {
        private readonly SpeechCredentialsResult _result;

        public FakeSpeechCredentialsProvider(SpeechCredentialsResult result)
        {
            _result = result;
        }

        public SpeechCredentialsResult GetCredentials(string? preferredRegion = null, string? preferredKey = null) => _result;
    }

    private sealed class FakeAzureAiServiceSettingsStore : IAzureAiServiceSettingsStore
    {
        public AzureAiServiceSettings? LoadedSettings { get; init; }
        public AzureAiServiceSettings? SavedSettings { get; private set; }
        public int SaveCallCount { get; private set; }

        public Task<AzureAiServiceSettings?> LoadAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(LoadedSettings);
        }

        public Task SaveAsync(AzureAiServiceSettings settings, CancellationToken cancellationToken = default)
        {
            SaveCallCount++;
            SavedSettings = settings;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSecretProtector : ISecretProtector
    {
        public byte[] Protect(string plaintext) => Encoding.UTF8.GetBytes(plaintext);

        public string Unprotect(byte[] protectedData) => Encoding.UTF8.GetString(protectedData);
    }

    private sealed class FakeTranslationController : ITranslationController
    {
        public int StartCallCount { get; private set; }
        public int StopCallCount { get; private set; }
        public bool IsRunning { get; private set; }
        public bool StartShouldYield { get; init; }
        public Exception? StopException { get; init; }
        public bool KeepRunningOnStopFailure { get; init; }
        public SpeechCredentials? LastStartCredentials { get; private set; }

        public Task StartAsync(SpeechCredentials credentials, string sourceLanguage, string targetLanguage, TranslationRecognizerWorkerBase worker, CancellationToken cancellationToken = default)
        {
            return StartAsyncCore();

            async Task StartAsyncCore()
            {
                if (StartShouldYield)
                {
                    await Task.Yield();
                }

                StartCallCount++;
                LastStartCredentials = credentials;
                IsRunning = true;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            return StopAsyncCore();

            Task StopAsyncCore()
            {
                StopCallCount++;

                if (StopException is not null)
                {
                    if (!KeepRunningOnStopFailure)
                    {
                        IsRunning = false;
                    }

                    throw StopException;
                }

                IsRunning = false;
                return Task.CompletedTask;
            }
        }
    }

    private sealed class FakeDesktopTranslationWorkerFactory : IDesktopTranslationWorkerFactory
    {
        private readonly IDesktopTranslationWorker _worker;
        public int CreateCallCount { get; private set; }

        public FakeDesktopTranslationWorkerFactory(IDesktopTranslationWorker worker)
        {
            _worker = worker;
        }

        public IDesktopTranslationWorker Create(string targetLanguage, string? recordingFileName)
        {
            CreateCallCount++;
            return _worker;
        }
    }

    private sealed class FakeRecordingFileService : IRecordingFileService
    {
        public Exception? NormalizeFileNameException { get; init; }

        public string? NormalizeFileName(string? fileName)
        {
            if (NormalizeFileNameException is not null)
            {
                throw NormalizeFileNameException;
            }

            return string.IsNullOrWhiteSpace(fileName) ? null : fileName.Trim();
        }

        public void AppendTranslation(string? fileName, string sourceText, string translatedText)
        {
        }
    }

    private sealed class FakeDesktopTranslationWorker : IDesktopTranslationWorker
    {
        public TranslationRecognizerWorkerBase RecognizerWorker { get; } = new NoOpTranslationRecognizerWorker();

        public event EventHandler<string>? MessageLogged;

        public event EventHandler<WorkerStatusChangedEventArgs>? StatusChanged;

        public event EventHandler<TranslationLogItem>? TranslationLogged;

        public void RaiseMessageLogged(string message) => MessageLogged?.Invoke(this, message);

        public void RaiseStatusChanged(DesktopTranslationStatus status, string message) =>
            StatusChanged?.Invoke(this, new WorkerStatusChangedEventArgs(status, message));

        public void RaiseTranslationLogged(TranslationLogItem item) => TranslationLogged?.Invoke(this, item);
    }

    private sealed class NoOpTranslationRecognizerWorker : TranslationRecognizerWorkerBase
    {
        public override void OnCanceled(TranslationRecognitionCanceledEventArgs e)
        {
        }

        public override void OnRecognized(TranslationRecognitionEventArgs e)
        {
        }

        public override void OnRecognizing(TranslationRecognitionEventArgs e)
        {
        }

        public override void OnSessionStarted(SessionEventArgs e)
        {
        }

        public override void OnSessionStopped(SessionEventArgs e)
        {
        }

        public override void OnSpeechEndDetected(RecognitionEventArgs e)
        {
        }

        public override void OnSpeechStartDetected(RecognitionEventArgs e)
        {
        }
    }

    private sealed class SynchronizationContextDispatcher : IUiDispatcher
    {
        private readonly SynchronizationContext _synchronizationContext;

        public SynchronizationContextDispatcher(SynchronizationContext synchronizationContext)
        {
            _synchronizationContext = synchronizationContext;
        }

        public void Invoke(Action action)
        {
            if (SynchronizationContext.Current == _synchronizationContext)
            {
                action();
                return;
            }

            _synchronizationContext.Send(_ => action(), null);
        }
    }

    private static Task RunOnSynchronizationContextAsync(Func<int, Task> testAction)
    {
        var completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            Exception? exception = null;
            var synchronizationContext = new PumpingSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(synchronizationContext);

            try
            {
                var task = testAction(Environment.CurrentManagedThreadId);
                task.ContinueWith(
                    completedTask =>
                    {
                        exception = completedTask.Exception?.GetBaseException();
                        synchronizationContext.Complete();
                    },
                    CancellationToken.None,
                    TaskContinuationOptions.None,
                    TaskScheduler.Default);
                synchronizationContext.RunOnCurrentThread();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(null);
            }

            if (exception is not null)
            {
                completionSource.SetException(exception);
                return;
            }

            completionSource.SetResult();
        });

        thread.IsBackground = true;
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        return completionSource.Task;
    }

    private sealed class PumpingSynchronizationContext : SynchronizationContext
    {
        private readonly Queue<(SendOrPostCallback Callback, object? State)> _workItems = new();
        private readonly AutoResetEvent _workItemsWaiting = new(false);
        private readonly int _threadId = Environment.CurrentManagedThreadId;
        private bool _completed;

        public override void Post(SendOrPostCallback d, object? state)
        {
            lock (_workItems)
            {
                _workItems.Enqueue((d, state));
            }

            _workItemsWaiting.Set();
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
            if (Environment.CurrentManagedThreadId == _threadId)
            {
                d(state);
                return;
            }

            using var completed = new ManualResetEventSlim();
            Exception? capturedException = null;
            Post(_ =>
            {
                try
                {
                    d(state);
                }
                catch (Exception ex)
                {
                    capturedException = ex;
                }
                finally
                {
                    completed.Set();
                }
            }, null);

            completed.Wait();

            if (capturedException is not null)
            {
                throw capturedException;
            }
        }

        public void Complete()
        {
            _completed = true;
            _workItemsWaiting.Set();
        }

        public void RunOnCurrentThread()
        {
            while (true)
            {
                (SendOrPostCallback Callback, object? State)? workItem = null;

                lock (_workItems)
                {
                    if (_workItems.Count > 0)
                    {
                        workItem = _workItems.Dequeue();
                    }
                    else if (_completed)
                    {
                        return;
                    }
                }

                if (workItem is { } item)
                {
                    item.Callback(item.State);
                    continue;
                }

                _workItemsWaiting.WaitOne();
            }
        }
    }
}