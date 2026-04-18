using SpeechTranslatorConsole;

namespace SpeechTranslatorShared.Tests;

public class TranslationRecognizerWorkerTest
{
    [Fact]
    public void WriteTranslatedSpeech_WritesRecordingFile_WhenFileNameIsProvided()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            var worker = new TranslationRecognizerWorker(directory, "recording");

            worker.WriteTranslatedSpeech(
                "hello",
                new Dictionary<string, string>
                {
                    ["ja-JP"] = "こんにちは"
                });

            var lines = File.ReadAllLines(Path.Combine(directory, "recording.txt"));

            lines.Should().Equal("hello", "こんにちは", "");
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public void WriteTranslatedSpeech_DoesNotWriteRecordingFile_WhenFileNameIsEmpty()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            var worker = new TranslationRecognizerWorker(directory, string.Empty);

            worker.WriteTranslatedSpeech(
                "hello",
                new Dictionary<string, string>
                {
                    ["ja-JP"] = "こんにちは"
                });

            File.Exists(Path.Combine(directory, ".txt")).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
