using SpeechTranslatorDesktop.Services;
using SpeechTranslatorDesktop.ViewModels;

namespace SpeechTranslatorDesktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var recordingFileService = new RecordingFileService(AppContext.BaseDirectory);
        DataContext = new MainViewModel(
            new WpfUiDispatcher(),
            new EnvironmentSpeechCredentialsProvider(),
            recordingFileService,
            new DesktopTranslationController(),
            new DesktopTranslationWorkerFactory(recordingFileService));
    }
}
