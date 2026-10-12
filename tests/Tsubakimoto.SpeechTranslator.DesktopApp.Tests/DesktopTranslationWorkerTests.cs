using Tsubakimoto.SpeechTranslator.DesktopApp.Models;
using Tsubakimoto.SpeechTranslator.DesktopApp.Services;

namespace Tsubakimoto.SpeechTranslator.DesktopApp.Tests;

public class DesktopTranslationWorkerTests
{
    [Fact]
    public void HandleTranslatedSpeech_WhenRecordingWriteFails_RaisesErrorWithoutThrowing()
    {
        // Arrange
        var recordingFileService = new ThrowingRecordingFileService(new IOException("disk full"));
        var worker = new DesktopTranslationWorker("ja-JP", "session-01", recordingFileService);
        var statuses = new List<WorkerStatusChangedEventArgs>();
        var translations = new List<TranslationLogItem>();

        worker.StatusChanged += (_, e) => statuses.Add(e);
        worker.TranslationLogged += (_, e) => translations.Add(e);

        // Act
        var act = () => worker.HandleTranslatedSpeech("hello", "こんにちは");

        // Assert
        Assert.Null(Record.Exception(act));
        Assert.Single(translations);
        Assert.Single(statuses, e =>
            e.Status == DesktopTranslationStatus.Error &&
            e.Message.Contains("記録ファイルの保存に失敗しました") &&
            e.Message.Contains("disk full"));
        Assert.DoesNotContain(statuses, e => e.Status == DesktopTranslationStatus.TranslatedSpeech);
    }

    private sealed class ThrowingRecordingFileService : IRecordingFileService
    {
        private readonly Exception _exception;

        public ThrowingRecordingFileService(Exception exception)
        {
            _exception = exception;
        }

        public string? NormalizeFileName(string? fileName) => fileName;

        public void AppendTranslation(string? fileName, string sourceText, string translatedText)
        {
            throw _exception;
        }
    }
}