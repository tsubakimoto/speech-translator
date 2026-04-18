using System.Text;

namespace SpeechTranslator.Desktop;

public interface IRecordingWriter : IDisposable
{
    void Write(TranslationEntry entry);
}

public interface IRecordingWriterFactory
{
    IRecordingWriter Create(string filePath);
}

public sealed class RecordingWriterFactory : IRecordingWriterFactory
{
    public IRecordingWriter Create(string filePath) => new RecordingWriter(filePath);
}

public sealed class RecordingWriter : IRecordingWriter
{
    private readonly object _gate = new();
    private readonly StreamWriter _writer;

    public RecordingWriter(string filePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        _writer = new StreamWriter(filePath, append: true, Encoding.UTF8);
    }

    public void Write(TranslationEntry entry)
    {
        lock (_gate)
        {
            _writer.WriteLine(entry.Timestamp.ToString("O"));
            _writer.WriteLine(entry.SourceText);
            _writer.WriteLine(entry.TranslatedText);
            _writer.WriteLine();
            _writer.Flush();
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _writer.Dispose();
        }
    }
}
