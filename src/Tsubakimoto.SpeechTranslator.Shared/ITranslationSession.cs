namespace Tsubakimoto.SpeechTranslator.Shared;

public interface ITranslationSession : IAsyncDisposable
{
    Task Completion { get; }

    bool IsRunning { get; }

    Task StopAsync();
}
