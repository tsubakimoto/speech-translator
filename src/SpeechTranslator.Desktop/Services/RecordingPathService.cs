namespace SpeechTranslator.Desktop;

public interface IRecordingPathService
{
    string? GetRecordingFilePath(string? recordingName);

    string? GetRecordingFilePath(string? recordingDirectory, string? recordingName);
}

public sealed class RecordingPathService : IRecordingPathService
{
    private static readonly HashSet<string> ReservedDeviceNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON",
        "PRN",
        "AUX",
        "NUL",
        "COM1",
        "COM2",
        "COM3",
        "COM4",
        "COM5",
        "COM6",
        "COM7",
        "COM8",
        "COM9",
        "LPT1",
        "LPT2",
        "LPT3",
        "LPT4",
        "LPT5",
        "LPT6",
        "LPT7",
        "LPT8",
        "LPT9"
    };

    private readonly string _documentsPath;

    public RecordingPathService(string? documentsPath = null)
    {
        _documentsPath = documentsPath ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }

    public string GetDefaultRecordingDirectory() => Path.Combine(_documentsPath, "recordings");

    public string? GetRecordingFilePath(string? recordingName)
        => GetRecordingFilePath(null, recordingName);

    public string? GetRecordingFilePath(string? recordingDirectory, string? recordingName)
    {
        if (string.IsNullOrWhiteSpace(recordingName))
        {
            return null;
        }

        var safeName = string.Concat(recordingName.Trim().Select(ToSafeFileNameCharacter));
        safeName = safeName.TrimEnd(' ', '.');
        if (string.IsNullOrWhiteSpace(safeName))
        {
            return null;
        }

        if (IsReservedDeviceName(safeName))
        {
            return null;
        }

        var baseDirectory = string.IsNullOrWhiteSpace(recordingDirectory)
            ? GetDefaultRecordingDirectory()
            : recordingDirectory.Trim();

        return Path.Combine(baseDirectory, $"{safeName}.txt");
    }

    private static char ToSafeFileNameCharacter(char character)
    {
        return char.IsLetterOrDigit(character) || character is ' ' or '-' or '_' ? character : '_';
    }

    private static bool IsReservedDeviceName(string fileName)
    {
        var baseName = fileName;
        var dotIndex = baseName.IndexOf('.');
        if (dotIndex >= 0)
        {
            baseName = baseName[..dotIndex];
        }

        baseName = baseName.TrimEnd(' ', '.');
        return baseName.Length > 0 && ReservedDeviceNames.Contains(baseName);
    }
}
