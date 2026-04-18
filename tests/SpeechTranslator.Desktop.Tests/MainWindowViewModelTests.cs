using System.Collections.Generic;

namespace SpeechTranslator.Desktop.Tests;

public class MainWindowViewModelTests
{
    [Fact]
    public async Task StartAsync_UsesSelectedMicrophoneAndRecordingDirectory()
    {
        var settingsStore = new FakeSettingsStore();
        var translatorService = new FakeTranslatorService();
        var recordingWriterFactory = new FakeRecordingWriterFactory();
        var microphoneService = new FakeMicrophoneDeviceService(
            new MicrophoneDeviceOption("USB Mic", "USB Mic"),
            new MicrophoneDeviceOption("Conference Mic", "Conference Mic"));
        var viewModel = new MainWindowViewModel(
            new ImmediateDispatcher(),
            settingsStore,
            new RecordingPathService("C:\\Users\\test\\Documents"),
            translatorService,
            recordingWriterFactory,
            microphoneService,
            new FakeRecordingDirectoryPicker())
        {
            Region = "japaneast",
            SubscriptionKey = "secret-key",
            RecordingName = "demo",
            RecordingDirectory = "C:\\Users\\test\\Desktop\\captures",
            SelectedSourceLanguage = new LanguageOption("en-US", "English (en-US)"),
            SelectedTargetLanguage = new LanguageOption("ja-JP", "Japanese (ja-JP)"),
            SelectedMicrophoneDevice = null
        };

        viewModel.SelectedMicrophoneDevice = viewModel.MicrophoneDevices[1];

        await viewModel.StartAsync();

        translatorService.StartedOptions.Should().NotBeNull();
        translatorService.StartedOptions!.Region.Should().Be("japaneast");
        translatorService.StartedOptions.SubscriptionKey.Should().Be("secret-key");
        translatorService.StartedOptions.MicrophoneDeviceName.Should().Be("USB Mic");
        recordingWriterFactory.CreatedPath.Should().Be("C:\\Users\\test\\Desktop\\captures\\demo.txt");
        settingsStore.SavedSettings.Should().NotBeNull();
        settingsStore.SavedSettings!.Region.Should().Be("japaneast");
        settingsStore.SavedSettings.SourceLanguage.Should().Be("en-US");
        settingsStore.SavedSettings.TargetLanguage.Should().Be("ja-JP");
        settingsStore.SavedSettings.RecordingName.Should().Be("demo");
        settingsStore.SavedSettings.RecordingDirectory.Should().Be("C:\\Users\\test\\Desktop\\captures");
        settingsStore.SavedSettings.MicrophoneDeviceName.Should().Be("USB Mic");
        viewModel.IsRunning.Should().BeTrue();
    }

    [Fact]
    public void SelectRecordingDirectoryCommand_UpdatesRecordingDirectory()
    {
        var viewModel = new MainWindowViewModel(
            new ImmediateDispatcher(),
            new FakeSettingsStore(),
            new RecordingPathService("C:\\Users\\test\\Documents"),
            new FakeTranslatorService(),
            microphoneDeviceService: new FakeMicrophoneDeviceService(new MicrophoneDeviceOption(string.Empty, "Default microphone")),
            recordingDirectoryPicker: new FakeRecordingDirectoryPicker("C:\\Users\\test\\Desktop\\captures"))
        {
            Region = "westeurope",
            SubscriptionKey = "secret-key",
            SelectedSourceLanguage = new LanguageOption("en-US", "English (en-US)"),
            SelectedTargetLanguage = new LanguageOption("ja-JP", "Japanese (ja-JP)")
        };

        viewModel.SelectRecordingDirectoryCommand.Execute(null);

        viewModel.RecordingDirectory.Should().Be("C:\\Users\\test\\Desktop\\captures");
    }

    [Fact]
    public void SavePreferences_PersistsLastSelectedNonSensitiveSettings()
    {
        var settingsStore = new FakeSettingsStore();
        var viewModel = new MainWindowViewModel(
            new ImmediateDispatcher(),
            settingsStore,
            new RecordingPathService("C:\\Users\\test\\Documents"),
            new FakeTranslatorService(),
            microphoneDeviceService: new FakeMicrophoneDeviceService(new MicrophoneDeviceOption(string.Empty, "Default microphone")))
        {
            Region = "westeurope",
            RecordingName = "final-cut",
            RecordingDirectory = "C:\\Users\\test\\Desktop\\captures",
            SelectedSourceLanguage = new LanguageOption("en-US", "English (en-US)"),
            SelectedTargetLanguage = new LanguageOption("ja-JP", "Japanese (ja-JP)")
        };

        viewModel.SavePreferences();

        settingsStore.SavedSettings.Should().NotBeNull();
        settingsStore.SavedSettings!.Region.Should().Be("westeurope");
        settingsStore.SavedSettings.SourceLanguage.Should().Be("en-US");
        settingsStore.SavedSettings.TargetLanguage.Should().Be("ja-JP");
        settingsStore.SavedSettings.RecordingName.Should().Be("final-cut");
        settingsStore.SavedSettings.RecordingDirectory.Should().Be("C:\\Users\\test\\Desktop\\captures");
    }

    [Fact]
    public async Task StartAsync_RejectsInvalidRecordingName()
    {
        var settingsStore = new FakeSettingsStore();
        var translatorService = new FakeTranslatorService();
        var viewModel = new MainWindowViewModel(
            new ImmediateDispatcher(),
            settingsStore,
            new RecordingPathService("C:\\Users\\test\\Documents"),
            translatorService,
            microphoneDeviceService: new FakeMicrophoneDeviceService(new MicrophoneDeviceOption(string.Empty, "Default microphone")))
        {
            Region = "japaneast",
            SubscriptionKey = "secret-key",
            RecordingName = "CON",
            SelectedSourceLanguage = new LanguageOption("en-US", "English (en-US)"),
            SelectedTargetLanguage = new LanguageOption("ja-JP", "Japanese (ja-JP)")
        };

        await viewModel.StartAsync();

        translatorService.StartedOptions.Should().BeNull();
        viewModel.StatusMessage.Should().Be("Recording name is invalid.");
    }

    [Fact]
    public async Task StartAsync_HandlesInvalidRecordingDirectory()
    {
        var settingsStore = new FakeSettingsStore();
        var translatorService = new FakeTranslatorService();
        var viewModel = new MainWindowViewModel(
            new ImmediateDispatcher(),
            settingsStore,
            new ThrowingRecordingPathService(),
            translatorService,
            microphoneDeviceService: new FakeMicrophoneDeviceService(new MicrophoneDeviceOption(string.Empty, "Default microphone")))
        {
            Region = "japaneast",
            SubscriptionKey = "secret-key",
            RecordingName = "demo",
            RecordingDirectory = "C:\\Users\\test\\Desktop\\captures|invalid",
            SelectedSourceLanguage = new LanguageOption("en-US", "English (en-US)"),
            SelectedTargetLanguage = new LanguageOption("ja-JP", "Japanese (ja-JP)")
        };

        await viewModel.StartAsync();

        translatorService.StartedOptions.Should().BeNull();
        viewModel.IsRunning.Should().BeFalse();
        viewModel.StatusMessage.Should().StartWith("Unable to create recording file:");
    }

    [Fact]
    public void SavePreferences_NormalizesNullSettingsLoadedFromStore()
    {
        var settingsStore = new FakeSettingsStore(
            new DesktopSettings(null!, "en-US", "ja-JP", null!));
        var viewModel = new MainWindowViewModel(
            new ImmediateDispatcher(),
            settingsStore,
            new RecordingPathService("C:\\Users\\test\\Documents"),
            new FakeTranslatorService(),
            microphoneDeviceService: new FakeMicrophoneDeviceService(new MicrophoneDeviceOption(string.Empty, "Default microphone")));

        Action save = viewModel.SavePreferences;

        save.Should().NotThrow();
        viewModel.Region.Should().Be(string.Empty);
        viewModel.RecordingName.Should().Be(string.Empty);
        viewModel.RecordingDirectory.Should().Be("C:\\Users\\test\\Documents\\recordings");
        settingsStore.SavedSettings.Should().Be(new DesktopSettings(string.Empty, "en-US", "ja-JP", string.Empty, "C:\\Users\\test\\Documents\\recordings", string.Empty));
    }

    [Fact]
    public void TrySavePreferences_ReturnsFalseWhenStoreThrowsRelevantIOException()
    {
        var viewModel = new MainWindowViewModel(
            new ImmediateDispatcher(),
            new ThrowingSettingsStore(),
            new RecordingPathService("C:\\Users\\test\\Documents"),
            new FakeTranslatorService(),
            microphoneDeviceService: new FakeMicrophoneDeviceService(new MicrophoneDeviceOption(string.Empty, "Default microphone")))
        {
            Region = "westeurope",
            RecordingName = "demo",
            SelectedSourceLanguage = new LanguageOption("en-US", "English (en-US)"),
            SelectedTargetLanguage = new LanguageOption("ja-JP", "Japanese (ja-JP)")
        };

        var saved = viewModel.TrySavePreferences();

        saved.Should().BeFalse();
        viewModel.StatusMessage.Should().StartWith("Failed to save preferences:");
    }

    [Fact]
    public async Task StartAsync_ContinuesWhenPreferenceSavingFails()
    {
        var settingsStore = new ThrowingSettingsStore();
        var translatorService = new FakeTranslatorService();
        var recordingWriterFactory = new FakeRecordingWriterFactory();
        var viewModel = new MainWindowViewModel(
            new ImmediateDispatcher(),
            settingsStore,
            new RecordingPathService("C:\\Users\\test\\Documents"),
            translatorService,
            recordingWriterFactory,
            new FakeMicrophoneDeviceService(new MicrophoneDeviceOption(string.Empty, "Default microphone")))
        {
            Region = "japaneast",
            SubscriptionKey = "secret-key",
            RecordingName = "demo",
            SelectedSourceLanguage = new LanguageOption("en-US", "English (en-US)"),
            SelectedTargetLanguage = new LanguageOption("ja-JP", "Japanese (ja-JP)")
        };

        await viewModel.StartAsync();

        translatorService.StartedOptions.Should().NotBeNull();
        viewModel.IsRunning.Should().BeTrue();
        viewModel.StatusMessage.Should().StartWith("Failed to save preferences:");
    }

    [Fact]
    public async Task MonitorSessionAsync_PreservesSdkCancellationMessage()
    {
        var recordingDirectory = CreateWritableDirectory();
        var settingsStore = new FakeSettingsStore();
        var translatorService = new FakeTranslatorService();
        var viewModel = new MainWindowViewModel(
            new ImmediateDispatcher(),
            settingsStore,
            new RecordingPathService("C:\\Users\\test\\Documents"),
            translatorService,
            microphoneDeviceService: new FakeMicrophoneDeviceService(new MicrophoneDeviceOption(string.Empty, "Default microphone")))
        {
            Region = "japaneast",
            SubscriptionKey = "secret-key",
            RecordingName = "demo",
            RecordingDirectory = recordingDirectory,
            SelectedSourceLanguage = new LanguageOption("en-US", "English (en-US)"),
            SelectedTargetLanguage = new LanguageOption("ja-JP", "Japanese (ja-JP)")
        };

        await viewModel.StartAsync();
        viewModel.SetStatusMessage("Canceled: Error");
        translatorService.CompleteSession();

        await WaitForAsync(() => !viewModel.IsRunning);

        viewModel.StatusMessage.Should().Be("Canceled: Error");
    }

    [Fact]
    public async Task StopAsync_ShowsTranslationStoppedForManualStop()
    {
        var recordingDirectory = CreateWritableDirectory();
        var settingsStore = new FakeSettingsStore();
        var translatorService = new FakeTranslatorService();
        var viewModel = new MainWindowViewModel(
            new ImmediateDispatcher(),
            settingsStore,
            new RecordingPathService("C:\\Users\\test\\Documents"),
            translatorService,
            microphoneDeviceService: new FakeMicrophoneDeviceService(new MicrophoneDeviceOption(string.Empty, "Default microphone")))
        {
            Region = "japaneast",
            SubscriptionKey = "secret-key",
            RecordingName = "demo",
            RecordingDirectory = recordingDirectory,
            SelectedSourceLanguage = new LanguageOption("en-US", "English (en-US)"),
            SelectedTargetLanguage = new LanguageOption("ja-JP", "Japanese (ja-JP)")
        };

        await viewModel.StartAsync();
        await viewModel.StopAsync();
        await WaitForAsync(() => !viewModel.IsRunning);

        viewModel.StatusMessage.Should().Be("Translation stopped.");
    }

    [Fact]
    public async Task StartAsync_AbortsWhenRecordingWriterCreationFails()
    {
        var settingsStore = new FakeSettingsStore();
        var translatorService = new FakeTranslatorService();
        var viewModel = new MainWindowViewModel(
            new ImmediateDispatcher(),
            settingsStore,
            new RecordingPathService("C:\\Users\\test\\Documents"),
            translatorService,
            new ThrowingRecordingWriterFactory(),
            new FakeMicrophoneDeviceService(new MicrophoneDeviceOption(string.Empty, "Default microphone")))
        {
            Region = "japaneast",
            SubscriptionKey = "secret-key",
            RecordingName = "demo",
            SelectedSourceLanguage = new LanguageOption("en-US", "English (en-US)"),
            SelectedTargetLanguage = new LanguageOption("ja-JP", "Japanese (ja-JP)")
        };

        await viewModel.StartAsync();

        translatorService.StartedOptions.Should().BeNull();
        viewModel.IsRunning.Should().BeFalse();
        viewModel.StatusMessage.Should().StartWith("Unable to create recording file:");
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(2);
        while (!condition())
        {
            if (DateTimeOffset.UtcNow >= deadline)
            {
                throw new TimeoutException("Timed out waiting for the condition to become true.");
            }

            await Task.Delay(10);
        }
    }

    private static string CreateWritableDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private sealed class ImmediateDispatcher : IUiDispatcher
    {
        public void Post(Action action) => action();
    }

    private sealed class FakeSettingsStore : IDesktopSettingsStore
    {
        private readonly DesktopSettings _loadedSettings;

        public FakeSettingsStore(DesktopSettings? loadedSettings = null)
        {
            _loadedSettings = loadedSettings ?? DesktopSettings.CreateDefault();
        }

        public DesktopSettings? SavedSettings { get; private set; }

        public DesktopSettings Load() => _loadedSettings;

        public void Save(DesktopSettings settings) => SavedSettings = settings;
    }

    private sealed class ThrowingSettingsStore : IDesktopSettingsStore
    {
        public DesktopSettings Load() => DesktopSettings.CreateDefault();

        public void Save(DesktopSettings settings) => throw new IOException("disk full");
    }

    private sealed class ThrowingRecordingPathService : IRecordingPathService
    {
        public string? GetRecordingFilePath(string? recordingName) => GetRecordingFilePath(null, recordingName);

        public string? GetRecordingFilePath(string? recordingDirectory, string? recordingName)
        {
            if (!string.IsNullOrWhiteSpace(recordingDirectory) && recordingDirectory.Contains('|'))
            {
                throw new ArgumentException("The path contains invalid characters.");
            }

            return Path.Combine(recordingDirectory ?? string.Empty, $"{recordingName}.txt");
        }
    }

    private sealed class FakeRecordingWriterFactory : IRecordingWriterFactory
    {
        public string? CreatedPath { get; private set; }

        public IRecordingWriter Create(string filePath)
        {
            CreatedPath = filePath;
            return new NoOpRecordingWriter();
        }
    }

    private sealed class ThrowingRecordingWriterFactory : IRecordingWriterFactory
    {
        public IRecordingWriter Create(string filePath) => throw new UnauthorizedAccessException("access denied");
    }

    private sealed class NoOpRecordingWriter : IRecordingWriter
    {
        public void Dispose()
        {
        }

        public void Write(TranslationEntry entry)
        {
        }
    }

    private sealed class FakeTranslatorService : ITranslatorService
    {
        public bool IsRunning { get; private set; }

        public TranslationSessionOptions? StartedOptions { get; private set; }
        private readonly TaskCompletionSource<object?> _session = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task StartAsync(TranslationSessionOptions options, TranslationRecognizerWorkerBase worker, CancellationToken cancellationToken = default)
        {
            StartedOptions = options;
            IsRunning = true;
            return _session.Task;
        }

        public Task StopAsync()
        {
            IsRunning = false;
            _session.TrySetResult(null);
            return Task.CompletedTask;
        }

        public void CompleteSession()
        {
            IsRunning = false;
            _session.TrySetResult(null);
        }
    }

    private sealed class FakeMicrophoneDeviceService : IMicrophoneDeviceService
    {
        public FakeMicrophoneDeviceService(params MicrophoneDeviceOption[] devices)
        {
            Devices = devices.Length == 0
                ? [new MicrophoneDeviceOption(string.Empty, "Default microphone")]
                : [..devices];
        }

        public IReadOnlyList<MicrophoneDeviceOption> Devices { get; }

        public IReadOnlyList<MicrophoneDeviceOption> GetInputDevices() => Devices;
    }

    private sealed class FakeRecordingDirectoryPicker : IRecordingDirectoryPicker
    {
        private readonly string? _selectedDirectory;

        public FakeRecordingDirectoryPicker(string? selectedDirectory = null)
        {
            _selectedDirectory = selectedDirectory;
        }

        public string? PickDirectory(string? initialDirectory) => _selectedDirectory;
    }
}
