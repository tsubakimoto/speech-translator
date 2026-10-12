using Tsubakimoto.SpeechTranslator.Shared;

namespace Tsubakimoto.SpeechTranslator.DesktopApp.Services;

public interface ITranslationController
{
    bool IsRunning { get; }

    Task StartAsync(SpeechCredentials credentials, string sourceLanguage, string targetLanguage, TranslationRecognizerWorkerBase worker, CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);
}
