namespace SpeechTranslatorDesktop.Services;

public enum DesktopTranslationStatus
{
    Started,
    Recognizing,
    RecognizedSpeech,
    TranslatedSpeech,
    Error,
    NoMatch,
    Canceled,
    SpeechStartDetected,
    SpeechEndDetected,
    SessionStarted,
    SessionStopped
}
