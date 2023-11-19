namespace SpeechTranslatorConsole;

internal record Settings(
    string Region,
    string SubscriptionKey,
    string FromLanguage,
    string TargetLanguage)
{
}
