namespace SpeechTranslatorDesktop.Services;

public interface IUiDispatcher
{
    void Invoke(Action action);
}
