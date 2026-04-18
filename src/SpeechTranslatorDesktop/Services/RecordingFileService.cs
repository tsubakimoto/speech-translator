using System.Text;

namespace SpeechTranslatorDesktop.Services;

public sealed class RecordingFileService : IRecordingFileService
{
    private static readonly HashSet<string> ReservedFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    private readonly string _rootDirectory;

    public RecordingFileService(string rootDirectory)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory))
        {
            throw new ArgumentException($"'{nameof(rootDirectory)}' を NULL または空にすることはできません。", nameof(rootDirectory));
        }

        _rootDirectory = rootDirectory;
    }

    public void AppendTranslation(string? fileName, string sourceText, string translatedText)
    {
        var safeFileName = NormalizeFileName(fileName);
        if (safeFileName is null)
        {
            return;
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(sourceText);
        ArgumentException.ThrowIfNullOrWhiteSpace(translatedText);

        var recordingsDirectory = Path.Combine(_rootDirectory, "recordings");
        var filePath = GetRecordingFilePath(recordingsDirectory, safeFileName);
        Directory.CreateDirectory(recordingsDirectory);

        using var streamWriter = new StreamWriter(filePath, append: true, Encoding.UTF8);
        streamWriter.WriteLine(sourceText);
        streamWriter.WriteLine(translatedText);
        streamWriter.WriteLine();
    }

    public string? NormalizeFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        return ValidateFileName(fileName);
    }

    private static string GetRecordingFilePath(string recordingsDirectory, string fileName)
    {
        var recordingsRootPath = Path.GetFullPath(recordingsDirectory);
        var filePath = Path.GetFullPath(Path.Combine(recordingsRootPath, $"{fileName}.txt"));
        var recordingsRootWithSeparator = recordingsRootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        if (!filePath.StartsWith(recordingsRootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("recordings 配下に保存できる単純なファイル名を指定してください。", nameof(fileName));
        }

        return filePath;
    }

    private static string ValidateFileName(string fileName)
    {
        var trimmedFileName = fileName.Trim();

        if (Path.IsPathRooted(trimmedFileName))
        {
            throw new ArgumentException("絶対パスは指定できません。", nameof(fileName));
        }

        if (trimmedFileName.Contains(Path.DirectorySeparatorChar) || trimmedFileName.Contains(Path.AltDirectorySeparatorChar))
        {
            throw new ArgumentException("ディレクトリ区切り文字は指定できません。", nameof(fileName));
        }

        if (trimmedFileName is "." or ".." || trimmedFileName.Contains("..", StringComparison.Ordinal))
        {
            throw new ArgumentException("親ディレクトリ参照は指定できません。", nameof(fileName));
        }

        if (trimmedFileName.Contains('.', StringComparison.Ordinal))
        {
            throw new ArgumentException("拡張子を含まない単純なファイル名を指定してください。", nameof(fileName));
        }

        if (trimmedFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("無効なファイル名です。", nameof(fileName));
        }

        if (!trimmedFileName.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))
        {
            throw new ArgumentException("ファイル名には英数字、ハイフン、アンダースコアのみ使用できます。", nameof(fileName));
        }

        if (ReservedFileNames.Contains(trimmedFileName))
        {
            throw new ArgumentException("予約済みのファイル名は指定できません。", nameof(fileName));
        }

        if (trimmedFileName.Length == 0)
        {
            throw new ArgumentException("ファイル名を指定してください。", nameof(fileName));
        }

        return trimmedFileName;
    }
}
