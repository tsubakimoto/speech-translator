namespace SpeechTranslatorDesktop.Services;

public sealed class DesktopTranslationWorkerFactory : IDesktopTranslationWorkerFactory
{
    private readonly IRecordingFileService _recordingFileService;

    public DesktopTranslationWorkerFactory(IRecordingFileService recordingFileService)
    {
        _recordingFileService = recordingFileService ?? throw new ArgumentNullException(nameof(recordingFileService));
    }

    public IDesktopTranslationWorker Create(string targetLanguage, string? recordingFileName)
    {
        return new DesktopTranslationWorker(targetLanguage, recordingFileName, _recordingFileService);
    }
}
