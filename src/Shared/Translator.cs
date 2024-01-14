using System.Text;

using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;

namespace Shared;

public class Translator
{
    private readonly SpeechTranslationConfig _speechTranslationConfig;

    public Action<TranslationRecognitionEventArgs> RecognizingAction { get; set; }

    public Action<TranslationRecognitionEventArgs> RecognizedAction { get; set; }

    public Translator(Uri endpointUrl, string subscriptionKey, string recognitionLanguage = "en-US", string targetLanguage = "ja-JP")
    {
        if (endpointUrl is null)
        {
            throw new ArgumentNullException(nameof(endpointUrl));
        }

        if (string.IsNullOrWhiteSpace(subscriptionKey))
        {
            throw new ArgumentException($"'{nameof(subscriptionKey)}' を NULL または空にすることはできません。", nameof(subscriptionKey));
        }

        if (string.IsNullOrWhiteSpace(recognitionLanguage))
        {
            throw new ArgumentException($"'{nameof(recognitionLanguage)}' を NULL または空にすることはできません。", nameof(recognitionLanguage));
        }

        if (string.IsNullOrWhiteSpace(targetLanguage))
        {
            throw new ArgumentException($"'{nameof(targetLanguage)}' を NULL または空にすることはできません。", nameof(targetLanguage));
        }

        _speechTranslationConfig = SpeechTranslationConfig.FromEndpoint(endpointUrl, subscriptionKey);
        _speechTranslationConfig.SpeechRecognitionLanguage = recognitionLanguage;
        _speechTranslationConfig.AddTargetLanguage(targetLanguage);
        _speechTranslationConfig.SetProperty(PropertyId.SpeechServiceConnection_TranslationVoice, "de-DE-Hedda");
    }

    public async Task MultiLingualTranslation(Action<string> outputAction)
    {
        var autoDetectSourceLanguageConfig = AutoDetectSourceLanguageConfig.FromLanguages([_speechTranslationConfig.SpeechRecognitionLanguage]);
        var stopTranslation = new TaskCompletionSource<int>();

        using (var audioInput = AudioConfig.FromDefaultMicrophoneInput())
        {
            using (var recognizer = new TranslationRecognizer(_speechTranslationConfig, autoDetectSourceLanguageConfig, audioInput))
            {
                recognizer.Recognizing += (s, e) =>
                {
                    if (RecognizingAction is not null)
                    {
                        RecognizingAction(e);
                    }
                };

                recognizer.Recognized += (s, e) =>
                {
                    if (RecognizedAction is not null)
                    {
                        RecognizedAction(e);
                    }
                };

                recognizer.Canceled += (s, e) =>
                {
                    outputAction($"CANCELED: Reason={e.Reason}");

                    if (e.Reason == CancellationReason.Error)
                    {
                        outputAction($"CANCELED: ErrorCode={e.ErrorCode}");
                        outputAction($"CANCELED: ErrorDetails={e.ErrorDetails}");
                        outputAction($"CANCELED: Did you set the speech resource key and region values?");
                    }

                    stopTranslation.TrySetResult(0);
                    outputAction(string.Empty);
                };

                recognizer.SpeechStartDetected += (s, e) => outputAction("Speech start detected event.");

                recognizer.SpeechEndDetected += (s, e) => outputAction("Speech end detected event.");

                recognizer.SessionStarted += (s, e) => outputAction("Session started event.");

                recognizer.SessionStopped += (s, e) =>
                {
                    outputAction("Session stopped event.");
                    outputAction("Stop translation.");
                    stopTranslation.TrySetResult(0);
                };

                // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
                outputAction("Start translation...");
                await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                Task.WaitAny(new[] { stopTranslation.Task });
                await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
            }
        }
    }
}
