namespace Tsubakimoto.SpeechTranslator.DesktopApp.Models;

public sealed record LanguageOption(string Code, string DisplayName)
{
    public override string ToString() => DisplayName;
}
