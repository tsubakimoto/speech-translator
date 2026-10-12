using System.Text;
using System.Text.RegularExpressions;
namespace Tsubakimoto.SpeechTranslator.WinUIApp.Core;
/// <summary>Validated UTF-8 append-only transcript recording.</summary>
public sealed class RecordingWriter : IRecordingWriter
{
    /// <inheritdoc />
    public string? Validate(string? folder, string filename)
    {
        if (string.IsNullOrWhiteSpace(filename)) return null;
        var stem = filename.Trim();
        if (!Regex.IsMatch(stem, "^[A-Za-z0-9_-]{1,200}$") || Regex.IsMatch(stem, "^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])$", RegexOptions.IgnoreCase))
            throw new ArgumentException("Use 1–200 ASCII letters, digits, - or _, without an extension or reserved name.");
        if (string.IsNullOrEmpty(folder) || !Path.IsPathFullyQualified(folder) || !Directory.Exists(folder)) throw new IOException("Select an existing recording folder.");
        var probe = Path.Combine(folder, ".speech-write-test-" + Guid.NewGuid().ToString("N"));
        using (new FileStream(probe, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose)) { }
        return Path.Combine(folder, stem + ".txt");
    }
    /// <inheritdoc />
    public void Append(string path, Translation translation)
    {
        using var writer = new StreamWriter(path, true, new UTF8Encoding(false));
        writer.WriteLine(translation.SourceText); writer.WriteLine(translation.TranslatedText); writer.WriteLine();
    }
}
