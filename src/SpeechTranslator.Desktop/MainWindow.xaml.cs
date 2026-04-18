using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;

namespace SpeechTranslator.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Closing += MainWindow_OnClosing;
        var settingsStore = new DesktopSettingsStore();
        var recordingPathService = new RecordingPathService();
        var translatorService = new TranslatorService();
        DataContext = new MainWindowViewModel(
            new WpfUiDispatcher(Dispatcher),
            settingsStore,
            recordingPathService,
            translatorService,
            null,
            new MicrophoneDeviceService(),
            new RecordingDirectoryPickerService());
    }

    private void SubscriptionKeyBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel && sender is PasswordBox passwordBox)
        {
            viewModel.SubscriptionKey = passwordBox.Password;
        }
    }

    private void MainWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.TrySavePreferences();
        }
    }
}
