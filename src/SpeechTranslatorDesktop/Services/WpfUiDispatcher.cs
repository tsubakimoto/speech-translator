namespace SpeechTranslatorDesktop.Services;

public sealed class WpfUiDispatcher : IUiDispatcher
{
    public void Invoke(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        Application.Current.Dispatcher.Invoke(action);
    }
}
