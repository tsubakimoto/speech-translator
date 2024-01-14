using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Translation;

namespace SpeechTranslatorShared;

public abstract class TranslationRecognizerWorkerBase
{
    public abstract void OnRecognizing(TranslationRecognitionEventArgs e);

    public abstract void OnRecognized(TranslationRecognitionEventArgs e);

    public abstract void OnCanceled(TranslationRecognitionCanceledEventArgs e);

    public abstract void OnSpeechStartDetected(RecognitionEventArgs e);

    public abstract void OnSpeechEndDetected(RecognitionEventArgs e);

    public abstract void OnSessionStarted(SessionEventArgs e);

    public abstract void OnSessionStopped(SessionEventArgs e);
}
