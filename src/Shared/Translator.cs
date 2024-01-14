using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;

namespace SpeechTranslatorShared;

public class Translator
{
    private readonly SpeechTranslationConfig _speechTranslationConfig;

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

    public async Task MultiLingualTranslation(TranslationRecognizerWorkerBase worker)
    {
        if (worker is null)
        {
            throw new ArgumentNullException(nameof(worker));
        }

        var autoDetectSourceLanguageConfig = AutoDetectSourceLanguageConfig.FromLanguages([_speechTranslationConfig.SpeechRecognitionLanguage]);
        var stopTranslation = new TaskCompletionSource<int>();

        using (var audioInput = AudioConfig.FromDefaultMicrophoneInput())
        using (var recognizer = new TranslationRecognizer(_speechTranslationConfig, autoDetectSourceLanguageConfig, audioInput))
        {
            recognizer.Recognizing += (s, e) => worker.OnRecognizing(e);

            recognizer.Recognized += (s, e) => worker.OnRecognized(e);

            recognizer.Canceled += (s, e) =>
            {
                stopTranslation.TrySetResult(0);
                worker.OnCanceled(e);
            };

            recognizer.SpeechStartDetected += (s, e) => worker.OnSpeechStartDetected(e);

            recognizer.SpeechEndDetected += (s, e) => worker.OnSpeechEndDetected(e);

            recognizer.SessionStarted += (s, e) => worker.OnSessionStarted(e);

            recognizer.SessionStopped += (s, e) =>
            {
                stopTranslation.TrySetResult(0);
                worker.OnSessionStopped(e);
            };

            // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
            await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

            Task.WaitAny(new[] { stopTranslation.Task });
            await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
        }
    }
}
