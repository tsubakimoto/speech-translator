namespace Tsubakimoto.SpeechTranslator.DesktopApp.Services;

public interface IDesktopTranslationWorkerFactory
{
    IDesktopTranslationWorker Create(string targetLanguage, string? recordingFileName);
}
