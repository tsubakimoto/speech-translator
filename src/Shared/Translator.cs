using Microsoft.CognitiveServices.Speech;

namespace Shared;

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
}
