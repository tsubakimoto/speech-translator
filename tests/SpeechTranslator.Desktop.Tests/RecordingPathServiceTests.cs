namespace SpeechTranslator.Desktop.Tests;

public class RecordingPathServiceTests
{
    [Fact]
    public void GetRecordingFilePath_ReturnsDocumentsRecordingsPath()
    {
        var service = new RecordingPathService("C:\\Users\\test\\Documents");

        var actual = service.GetRecordingFilePath("session-01");

        actual.Should().Be("C:\\Users\\test\\Documents\\recordings\\session-01.txt");
    }

    [Fact]
    public void GetRecordingFilePath_UsesSelectedRecordingDirectory()
    {
        var service = new RecordingPathService("C:\\Users\\test\\Documents");

        var actual = service.GetRecordingFilePath("C:\\Users\\test\\Desktop\\captures", "session-01");

        actual.Should().Be("C:\\Users\\test\\Desktop\\captures\\session-01.txt");
    }

    [Fact]
    public void GetRecordingFilePath_SanitizesInvalidCharacters()
    {
        var service = new RecordingPathService("C:\\Users\\test\\Documents");

        var actual = service.GetRecordingFilePath("C:\\Users\\test\\Desktop\\captures", "session:01/../");

        actual.Should().Be("C:\\Users\\test\\Desktop\\captures\\session_01____.txt");
    }

    [Fact]
    public void GetRecordingFilePath_ReturnsNullForBlankName()
    {
        var service = new RecordingPathService("C:\\Users\\test\\Documents");

        service.GetRecordingFilePath(" ").Should().BeNull();
    }

    [Fact]
    public void GetRecordingFilePath_ReturnsNullForWindowsReservedDeviceNames()
    {
        var service = new RecordingPathService("C:\\Users\\test\\Documents");

        service.GetRecordingFilePath("CON").Should().BeNull();
        service.GetRecordingFilePath("lpt9").Should().BeNull();
    }
}
