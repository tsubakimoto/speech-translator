namespace SpeechTranslatorDesktop.Services;

public static class SpeechSettingsPathProvider
{
    public static string GetDatabasePath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "SpeechTranslatorDesktop", "speech-translator-desktop.db");
    }
}
