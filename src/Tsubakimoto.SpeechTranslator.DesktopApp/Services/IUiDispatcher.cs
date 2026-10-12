namespace Tsubakimoto.SpeechTranslator.DesktopApp.Services;

public interface IUiDispatcher
{
    void Invoke(Action action);
}
