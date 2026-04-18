namespace SpeechTranslatorShared;

public class Translator
{
    private readonly SpeechTranslationConfig _speechTranslationConfig;
    private readonly string? _microphoneDeviceName;

    public Translator(Uri endpointUrl, string subscriptionKey, string recognitionLanguage = "en-US", string targetLanguage = "ja-JP", string? microphoneDeviceName = null)
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
        _speechTranslationConfig.SetProperty(PropertyId.SpeechServiceConnection_TranslationVoice, "en-US-JennyNeural");
        _microphoneDeviceName = string.IsNullOrWhiteSpace(microphoneDeviceName) ? null : microphoneDeviceName;
    }

    public async Task MultiLingualTranslation(TranslationRecognizerWorkerBase worker, CancellationToken cancellationToken = default)
    {
        if (worker is null)
        {
            throw new ArgumentNullException(nameof(worker));
        }

        var autoDetectSourceLanguageConfig = AutoDetectSourceLanguageConfig.FromLanguages([_speechTranslationConfig.SpeechRecognitionLanguage]);
        var stopTranslation = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        using (var audioInput = _microphoneDeviceName is null
            ? AudioConfig.FromDefaultMicrophoneInput()
            : AudioConfig.FromMicrophoneInput(_microphoneDeviceName))
        using (var recognizer = new TranslationRecognizer(_speechTranslationConfig, autoDetectSourceLanguageConfig, audioInput))
        {
            using var cancellationRegistration = cancellationToken.Register(() => stopTranslation.TrySetCanceled(cancellationToken));

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

            try
            {
                await stopTranslation.Task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
            }
        }
    }
}
