using System.Text;

using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Translation;

using SpeechTranslatorShared;

namespace SpeechTranslatorConsole;

internal class TranslationRecognizerWorker : TranslationRecognizerWorkerBase
{
    private readonly string _filePath;

    public TranslationRecognizerWorker(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException($"'{nameof(filePath)}' を null または空白にすることはできません。", nameof(filePath));
        }

        _filePath = filePath;
    }

    public override void OnRecognizing(TranslationRecognitionEventArgs e) => Console.Write(".");

    public override void OnRecognized(TranslationRecognitionEventArgs e)
    {
        Console.WriteLine();
        Console.WriteLine();

        var result = e.Result;

        if (result.Reason == ResultReason.TranslatedSpeech)
        {
            using (var sw = new StreamWriter(_filePath, true, Encoding.UTF8))
            {
                var lidResult = result.Properties.GetProperty(PropertyId.SpeechServiceConnection_AutoDetectSourceLanguageResult);

                Console.WriteLine($"{result.Text}");
                sw.WriteLine($"{result.Text}");
                foreach (var element in result.Translations)
                {
                    Console.WriteLine($"{element.Value}");
                    sw.WriteLine($"{element.Value}");
                }

                sw.WriteLine();
            }
        }
        else if (result.Reason == ResultReason.RecognizedSpeech)
        {
            Console.WriteLine($"RECOGNIZED: Text={result.Text}");
            Console.WriteLine($"    Speech not translated.");
        }
        else if (result.Reason == ResultReason.NoMatch)
        {
            Console.WriteLine($"NOMATCH: Speech could not be recognized.");
        }

        Console.WriteLine();
    }

    public override void OnCanceled(TranslationRecognitionCanceledEventArgs e)
    {
        Console.WriteLine($"CANCELED: Reason={e.Reason}");

        if (e.Reason == CancellationReason.Error)
        {
            Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
            Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
            Console.WriteLine($"CANCELED: Did you set the speech resource key and region values?");
        }

        Console.WriteLine();
    }

    public override void OnSpeechStartDetected(RecognitionEventArgs e) => Console.WriteLine("Speech start detected event.\n");

    public override void OnSpeechEndDetected(RecognitionEventArgs e) => Console.WriteLine("Speech end detected event.\n");

    public override void OnSessionStarted(SessionEventArgs e) => Console.WriteLine("Session started event.\n");

    public override void OnSessionStopped(SessionEventArgs e) => Console.WriteLine("Session stopped event.\nStop translation.\n");
}
