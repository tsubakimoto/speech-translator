using System.Windows.Input;
using SpeechTranslatorDesktop.Commands;
using SpeechTranslatorDesktop.Models;
using SpeechTranslatorDesktop.Services;
using SpeechTranslatorDesktop.ViewModels;
using SpeechTranslatorShared;

namespace SpeechTranslator.Desktop.Tests;

public class MainViewModelTests
{
    [Fact]
    public void InitialState_StartEnabled_StopDisabled()
    {
        var viewModel = CreateViewModel();

        viewModel.StartCommand.CanExecute(null).Should().BeTrue();
        viewModel.StopCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task Start_WhenCredentialsMissing_ShowsErrorAndDoesNotStart()
    {
        var translationController = new FakeTranslationController();
        var viewModel = CreateViewModel(
            credentialsProvider: new FakeSpeechCredentialsProvider(SpeechCredentialsResult.Failure("Missing SPEECH_REGION and SPEECH_KEY.")),
            translationController: translationController);

        await ExecuteAsync(viewModel.StartCommand);

        translationController.StartCallCount.Should().Be(0);
        viewModel.StatusMessage.Should().Contain("SPEECH_REGION").And.Contain("SPEECH_KEY");
        viewModel.IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task InitializeAsync_WhenSavedSettingsExist_LoadsThemIntoViewModel()
    {
        var viewModel = CreateViewModel(
            settingsStore: new FakeAzureAiServiceSettingsStore
            {
                LoadedSettings = new AzureAiServiceSettings("japaneast", "saved-key")
            });

        await viewModel.InitializeAsync();

        viewModel.AzureRegion.Should().Be("japaneast");
        viewModel.AzureApiKey.Should().Be("saved-key");
        viewModel.SettingsStatusMessage.Should().Contain("読み込み");
    }

    [Fact]
    public async Task InitializeAsync_WhenSavedSettingsDoNotExist_ShowsGuidance()
    {
        var viewModel = CreateViewModel();

        await viewModel.InitializeAsync();

        viewModel.AzureRegion.Should().BeEmpty();
        viewModel.AzureApiKey.Should().BeEmpty();
        viewModel.SettingsStatusMessage.Should().Contain("保存");
        viewModel.SettingsStatusMessage.Should().Contain("SPEECH_REGION");
    }

    [Fact]
    public async Task InitializeAsync_OnFirstLaunchWithMissingSettingsDatabase_ShowsGuidanceInsteadOfFailure()
    {
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

            await viewModel.InitializeAsync();

            viewModel.SettingsStatusMessage.Should().Contain("保存");
            viewModel.SettingsStatusMessage.Should().Contain("SPEECH_REGION");
            viewModel.SettingsStatusMessage.Should().NotContain("失敗");
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
    public async Task SaveSettings_WhenValuesAreValid_PersistsThem()
    {
        var settingsStore = new FakeAzureAiServiceSettingsStore();
        var viewModel = CreateViewModel(settingsStore: settingsStore);
        viewModel.AzureRegion = "japaneast";
        viewModel.AzureApiKey = "saved-key";

        await ExecuteAsync(viewModel.SaveSettingsCommand);

        settingsStore.SaveCallCount.Should().Be(1);
        settingsStore.SavedSettings.Should().BeEquivalentTo(new AzureAiServiceSettings("japaneast", "saved-key"));
        viewModel.SettingsStatusMessage.Should().Contain("保存");
    }

    [Fact]
    public async Task SaveSettings_WhenValuesAreMissing_ShowsValidationError()
    {
        var settingsStore = new FakeAzureAiServiceSettingsStore();
        var viewModel = CreateViewModel(settingsStore: settingsStore);
        viewModel.AzureRegion = "japaneast";
        viewModel.AzureApiKey = "";

        await ExecuteAsync(viewModel.SaveSettingsCommand);

        settingsStore.SaveCallCount.Should().Be(0);
        viewModel.SettingsStatusMessage.Should().Contain("API キー");
    }

    [Fact]
    public async Task Start_WhenCredentialsPresent_StartsTranslation()
    {
        var translationController = new FakeTranslationController();
        var viewModel = CreateViewModel(translationController: translationController);

        await ExecuteAsync(viewModel.StartCommand);

        translationController.StartCallCount.Should().Be(1);
        viewModel.StatusMessage.Should().Be("開始");
        viewModel.IsRunning.Should().BeTrue();
        viewModel.StartCommand.CanExecute(null).Should().BeFalse();
        viewModel.StopCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task Start_WhenConfiguredCredentialsPresent_PassesThemToController()
    {
        var translationController = new FakeTranslationController();
        var viewModel = CreateViewModel(
            credentialsProvider: new FakeSpeechCredentialsProvider(SpeechCredentialsResult.Success(new SpeechCredentials("ui-region", "ui-key"))),
            translationController: translationController);
        viewModel.AzureRegion = "ui-region";
        viewModel.AzureApiKey = "ui-key";

        await ExecuteAsync(viewModel.StartCommand);

        translationController.LastStartCredentials.Should().BeEquivalentTo(new SpeechCredentials("ui-region", "ui-key"));
    }

    [Fact]
    public async Task Start_WhenRecordingFileNameIsInvalid_ShowsErrorAndDoesNotStart()
    {
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

        await ExecuteAsync(viewModel.StartCommand);

        translationController.StartCallCount.Should().Be(0);
        workerFactory.CreateCallCount.Should().Be(0);
        viewModel.IsRunning.Should().BeFalse();
        viewModel.StatusMessage.Should().StartWith("記録ファイル名が不正です:");
        viewModel.ActivityLogs.Should().Contain(viewModel.StatusMessage);
    }

    [Fact]
    public async Task Stop_CallsTranslationController()
    {
        var translationController = new FakeTranslationController();
        var viewModel = CreateViewModel(translationController: translationController);

        await ExecuteAsync(viewModel.StartCommand);
        await ExecuteAsync(viewModel.StopCommand);

        translationController.StopCallCount.Should().Be(1);
        viewModel.StatusMessage.Should().Be("停止");
        viewModel.IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task Stop_WhenControllerThrows_ShowsErrorAndKeepsRunning()
    {
        var translationController = new FakeTranslationController
        {
            StopException = new InvalidOperationException("stop failed"),
            KeepRunningOnStopFailure = true
        };
        var viewModel = CreateViewModel(translationController: translationController);

        await ExecuteAsync(viewModel.StartCommand);
        await ExecuteAsync(viewModel.StopCommand);

        viewModel.StatusMessage.Should().Be("停止に失敗しました: stop failed");
        viewModel.ActivityLogs.Should().Contain("停止に失敗しました: stop failed");
        viewModel.IsRunning.Should().BeTrue();
        viewModel.StartCommand.CanExecute(null).Should().BeFalse();
        viewModel.StopCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task WorkerTranslationEvent_AddsTranslationLog()
    {
        var worker = new FakeDesktopTranslationWorker();
        var viewModel = CreateViewModel(workerFactory: new FakeDesktopTranslationWorkerFactory(worker));

        await ExecuteAsync(viewModel.StartCommand);
        worker.RaiseTranslationLogged(new TranslationLogItem("hello", "こんにちは"));

        viewModel.TranslationLogs.Should().ContainSingle();
        viewModel.TranslationLogs[0].SourceText.Should().Be("hello");
        viewModel.TranslationLogs[0].TranslatedText.Should().Be("こんにちは");
    }

    [Fact]
    public async Task WorkerStatusEvents_UpdateStatusAndLogs()
    {
        var worker = new FakeDesktopTranslationWorker();
        var viewModel = CreateViewModel(workerFactory: new FakeDesktopTranslationWorkerFactory(worker));

        await ExecuteAsync(viewModel.StartCommand);
        worker.RaiseStatusChanged(DesktopTranslationStatus.NoMatch, "NoMatch");
        worker.RaiseMessageLogged("Session stopped.");
        worker.RaiseStatusChanged(DesktopTranslationStatus.SessionStopped, "セッション停止");

        viewModel.StatusMessage.Should().Be("セッション停止");
        viewModel.ActivityLogs.Should().Contain("Session stopped.");
        viewModel.IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task Start_WhenControllerCompletesAsynchronously_RaisesPropertyChangesOnCapturedContext()
    {
        await RunOnSynchronizationContextAsync(async uiThreadId =>
        {
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

            await ExecuteAsync(viewModel.StartCommand);

            propertyChangedThreadIds.Should().NotBeEmpty();
            propertyChangedThreadIds.Should().OnlyContain(threadId => threadId == uiThreadId);
        });
    }

    [Fact]
    public async Task WorkerTranslationEvent_FromBackgroundThread_UpdatesCollectionOnUiThread()
    {
        await RunOnSynchronizationContextAsync(async uiThreadId =>
        {
            var worker = new FakeDesktopTranslationWorker();
            var viewModel = CreateViewModel(
                dispatcher: new SynchronizationContextDispatcher(SynchronizationContext.Current!),
                workerFactory: new FakeDesktopTranslationWorkerFactory(worker));
            int? collectionChangedThreadId = null;

            viewModel.TranslationLogs.CollectionChanged += (_, _) => collectionChangedThreadId = Environment.CurrentManagedThreadId;

            await ExecuteAsync(viewModel.StartCommand);
            await Task.Run(() => worker.RaiseTranslationLogged(new TranslationLogItem("hello", "こんにちは")));

            collectionChangedThreadId.Should().Be(uiThreadId);
            viewModel.TranslationLogs.Should().ContainSingle();
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
