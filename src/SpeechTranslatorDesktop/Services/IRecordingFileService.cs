namespace SpeechTranslatorDesktop.Services;

public interface IRecordingFileService
{
    string? NormalizeFileName(string? fileName);

    void AppendTranslation(string? fileName, string sourceText, string translatedText);
}
