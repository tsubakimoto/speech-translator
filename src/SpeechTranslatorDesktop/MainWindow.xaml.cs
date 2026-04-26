using SpeechTranslatorDesktop.Services;
using SpeechTranslatorDesktop.ViewModels;

namespace SpeechTranslatorDesktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var recordingFileService = new RecordingFileService(AppContext.BaseDirectory);
        var viewModel = new MainViewModel(
            new WpfUiDispatcher(),
            new EnvironmentSpeechCredentialsProvider(),
            new SqliteAzureAiServiceSettingsStore(
                SpeechSettingsPathProvider.GetDatabasePath(),
                new DpapiSecretProtector()),
            recordingFileService,
            new DesktopTranslationController(),
            new DesktopTranslationWorkerFactory(recordingFileService));
        DataContext = viewModel;
        Loaded += async (_, _) => await viewModel.InitializeAsync();
    }
}
