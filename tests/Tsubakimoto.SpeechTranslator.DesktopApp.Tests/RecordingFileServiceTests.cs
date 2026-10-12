using System.Text;

using Tsubakimoto.SpeechTranslator.DesktopApp.Services;

namespace Tsubakimoto.SpeechTranslator.DesktopApp.Tests;

public class RecordingFileServiceTests : IDisposable
{
    private readonly string _rootDirectory = Path.Combine(AppContext.BaseDirectory, "recording-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void AppendTranslation_FileNameProvided_WritesUtf8Recording()
    {
        // Arrange
        var service = new RecordingFileService(_rootDirectory);

        // Act
        service.AppendTranslation("session-01", "hello", "こんにちは");

        var filePath = Path.Combine(_rootDirectory, "recordings", "session-01.txt");
        // Assert
        Assert.True(File.Exists(filePath));
        var recording = File.ReadAllText(filePath, Encoding.UTF8);
        Assert.Contains("hello", recording);
        Assert.Contains("こんにちは", recording);
    }

    [Fact]
    public void AppendTranslation_EmptyFileName_DoesNotCreateRecording()
    {
        // Arrange
        var service = new RecordingFileService(_rootDirectory);

        // Act
        service.AppendTranslation(string.Empty, "hello", "こんにちは");

        // Assert
        Assert.False(Directory.Exists(Path.Combine(_rootDirectory, "recordings")));
    }

    [Theory]
    [InlineData(@"C:\escape")]
    [InlineData("..\\escape")]
    [InlineData("../escape")]
    [InlineData("nested/file")]
    [InlineData("nested\\file")]
    [InlineData("session.txt")]
    [InlineData("session 01")]
    [InlineData("..")]
    public void AppendTranslation_InvalidFileName_ThrowsArgumentException(string fileName)
    {
        // Arrange
        var service = new RecordingFileService(_rootDirectory);

        // Act
        var act = () => service.AppendTranslation(fileName, "hello", "こんにちは");

        // Assert
        Assert.ThrowsAny<ArgumentException>(act);
        Assert.False(Directory.Exists(Path.Combine(_rootDirectory, "recordings")));
    }

    [Fact]
    public void NormalizeFileName_ValidFileName_ReturnsTrimmedValue()
    {
        // Arrange
        var service = new RecordingFileService(_rootDirectory);

        // Act
        var normalized = service.NormalizeFileName(" session-01 ");

        // Assert
        Assert.Equal("session-01", normalized);
    }

    [Fact]
    public void NormalizeFileName_WhitespaceOnly_ReturnsNull()
    {
        // Arrange
        var service = new RecordingFileService(_rootDirectory);

        // Act
        var normalized = service.NormalizeFileName("   ");

        // Assert
        Assert.Null(normalized);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, recursive: true);
        }
    }
}