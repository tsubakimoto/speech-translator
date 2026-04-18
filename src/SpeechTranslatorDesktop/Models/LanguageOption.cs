namespace SpeechTranslatorDesktop.Models;

public sealed record LanguageOption(string Code, string DisplayName)
{
    public override string ToString() => DisplayName;
}
