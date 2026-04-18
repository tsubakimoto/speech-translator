namespace SpeechTranslator.Desktop;

public sealed record TranslationSessionOptions(
    string Region,
    string SubscriptionKey,
    string SourceLanguage,
    string TargetLanguage,
    string MicrophoneDeviceName = "");
