using Tsubakimoto.SpeechTranslator.DesktopApp.Models;
using Tsubakimoto.SpeechTranslator.Shared;

namespace Tsubakimoto.SpeechTranslator.DesktopApp.Services;

public interface IDesktopTranslationWorker
{
    TranslationRecognizerWorkerBase RecognizerWorker { get; }

    event EventHandler<string>? MessageLogged;

    event EventHandler<WorkerStatusChangedEventArgs>? StatusChanged;

    event EventHandler<TranslationLogItem>? TranslationLogged;
}
