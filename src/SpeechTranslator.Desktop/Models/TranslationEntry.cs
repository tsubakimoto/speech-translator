namespace SpeechTranslator.Desktop;

public sealed record TranslationEntry(DateTimeOffset Timestamp, string SourceText, string TranslatedText);
