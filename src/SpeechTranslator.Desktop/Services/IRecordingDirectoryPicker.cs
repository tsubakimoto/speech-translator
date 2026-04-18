namespace SpeechTranslator.Desktop;

public interface IRecordingDirectoryPicker
{
    string? PickDirectory(string? initialDirectory);
}
