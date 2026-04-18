using System;

namespace SpeechTranslator.Desktop;

public interface IUiDispatcher
{
    void Post(Action action);
}
