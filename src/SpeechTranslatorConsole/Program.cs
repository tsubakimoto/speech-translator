using System.Text;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;
using Microsoft.Extensions.Configuration;

using SpeechTranslatorConsole;

const string directoryName = "recordings";

var environmentName = Environment.GetEnvironmentVariable("DOTNETCORE_ENVIRONMENT") ?? "Development";

IConfigurationRoot config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

Settings? settings = config.GetRequiredSection(nameof(Settings)).Get<Settings>();
var region = settings?.Region ?? throw new ArgumentNullException("region");
var subscriptionKey = settings?.SubscriptionKey ?? throw new ArgumentNullException("subscriptionKey");
var fromLanguage = DetermineLanguage("speaker") ?? throw new ArgumentNullException("fromLanguage");
Console.WriteLine();
var targetLanguage = DetermineLanguage("translator") ?? throw new ArgumentNullException("targetLanguage");
Console.WriteLine();

Console.Write("Record file name: ");
var filePath = Console.ReadLine() ?? throw new ArgumentNullException("filePath");

if (!Directory.Exists(directoryName))
{
    Directory.CreateDirectory(directoryName);
    Console.WriteLine("The directory was created successfully at {0}.", Directory.GetCreationTime(directoryName));
}

var endpointString = $"wss://{region}.stt.speech.microsoft.com/speech/universal/v2";
var endpointUrl = new Uri(endpointString);
var speechTranslationConfig = SpeechTranslationConfig.FromEndpoint(endpointUrl, subscriptionKey);
speechTranslationConfig.SpeechRecognitionLanguage = fromLanguage;
speechTranslationConfig.AddTargetLanguage(targetLanguage);
speechTranslationConfig.SetProperty(PropertyId.SpeechServiceConnection_TranslationVoice, "de-DE-Hedda");

try
{
    await MultiLingualTranslation($"{directoryName}/{filePath}.txt", speechTranslationConfig);
}
catch (Exception e)
{
    Console.WriteLine("Exception: " + e.Message);
}
finally
{
    Console.WriteLine("Executing finally block.");
}

static string? DetermineLanguage(string title)
{
    Console.WriteLine("1. English (en-US)");
    Console.WriteLine("2. Japanese (ja-JP)");
    Console.Write($"Select {title} language: ");

    return Console.ReadLine() switch
    {
        "1" => "en-US",
        "2" => "ja-JP",
        _ => null
    };
}

static async Task MultiLingualTranslation(string filepath, SpeechTranslationConfig config)
{
    var autoDetectSourceLanguageConfig = AutoDetectSourceLanguageConfig.FromLanguages(new string[] { config.SpeechRecognitionLanguage });

    var stopTranslation = new TaskCompletionSource<int>();
    using (var audioInput = AudioConfig.FromDefaultMicrophoneInput())
    {
        using (var recognizer = new TranslationRecognizer(config, autoDetectSourceLanguageConfig, audioInput))
        {
            recognizer.Recognizing += (s, e) =>
            {
                Console.Write(".");

                //var lidResult = e.Result.Properties.GetProperty(PropertyId.SpeechServiceConnection_AutoDetectSourceLanguageResult);

                //Console.WriteLine($"RECOGNIZING in '{lidResult}': Text={e.Result.Text}");
                //foreach (var element in e.Result.Translations)
                //{
                //    Console.WriteLine($"    TRANSLATING into '{element.Key}': {element.Value}");
                //}
            };

            recognizer.Recognized += (s, e) =>
            {
                Console.WriteLine();
                Console.WriteLine();

                if (e.Result.Reason == ResultReason.TranslatedSpeech)
                {
                    var sw = new StreamWriter(filepath, true, Encoding.UTF8);

                    var lidResult = e.Result.Properties.GetProperty(PropertyId.SpeechServiceConnection_AutoDetectSourceLanguageResult);

                    // Console.WriteLine($"RECOGNIZED in '{lidResult}': Text={e.Result.Text}");
                    Console.WriteLine($"{e.Result.Text}");
                    sw.WriteLine($"{e.Result.Text}");
                    foreach (var element in e.Result.Translations)
                    {
                        // Console.WriteLine($"    TRANSLATED into '{element.Key}': {element.Value}");
                        Console.WriteLine($"{element.Value}");
                        sw.WriteLine($"{element.Value}");
                    }

                    sw.WriteLine();
                    sw.Close();
                }
                else if (e.Result.Reason == ResultReason.RecognizedSpeech)
                {
                    Console.WriteLine($"RECOGNIZED: Text={e.Result.Text}");
                    Console.WriteLine($"    Speech not translated.");
                }
                else if (e.Result.Reason == ResultReason.NoMatch)
                {
                    Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                }

                Console.WriteLine();
            };

            recognizer.Canceled += (s, e) =>
            {
                Console.WriteLine($"CANCELED: Reason={e.Reason}");

                if (e.Reason == CancellationReason.Error)
                {
                    Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                    Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                    Console.WriteLine($"CANCELED: Did you set the speech resource key and region values?");
                }

                stopTranslation.TrySetResult(0);
                Console.WriteLine();
            };

            recognizer.SpeechStartDetected += (s, e) =>
            {
                Console.WriteLine("\nSpeech start detected event.");
                Console.WriteLine();
            };

            recognizer.SpeechEndDetected += (s, e) =>
            {
                Console.WriteLine("\nSpeech end detected event.");
                Console.WriteLine();
            };

            recognizer.SessionStarted += (s, e) =>
            {
                Console.WriteLine("\nSession started event.");
                Console.WriteLine();
            };

            recognizer.SessionStopped += (s, e) =>
            {
                Console.WriteLine("\nSession stopped event.");
                Console.WriteLine($"\nStop translation.");
                Console.WriteLine();
                stopTranslation.TrySetResult(0);
            };

            // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
            Console.WriteLine("Start translation...");
            Console.WriteLine();
            await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

            Task.WaitAny(new[] { stopTranslation.Task });
            await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
        }
    }
}
