using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using SpeechTranslatorWinUI.Core;
namespace SpeechTranslatorWinUI;
/// <summary>Manual application composition and WinUI-only activation forwarding.</summary>
public partial class App : Application
{
    private AppInstance? _instance;
    /// <summary>The primary WinUI window.</summary>
    public static MainWindow Window { get; private set; } = null!;
    /// <summary>Initializes Fluent resources.</summary>
    public App() => InitializeComponent();
    /// <inheritdoc />
    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        _instance = AppInstance.FindOrRegisterForKey("SpeechTranslatorWinUI");
        if (!_instance.IsCurrent)
        {
            await _instance.RedirectActivationToAsync(AppInstance.GetCurrent().GetActivatedEventArgs()); Exit(); return;
        }
        var dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        var path = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "speech-translator-winui.db");
        var model = new MainViewModel(new SettingsStore(path, new DpapiSecretProtector()), new SpeechSession(), new RecordingWriter(), action => dispatcher.TryEnqueue(() => action()));
        Window = new MainWindow(model);
        _instance.Activated += (_, _) => dispatcher.TryEnqueue(() => Window.Activate());
        Window.Activate();
    }
}
