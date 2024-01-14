using Microsoft.Extensions.Configuration;

using Shared;

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
var endpointUrl = new Uri($"wss://{region}.stt.speech.microsoft.com/speech/universal/v2");
var subscriptionKey = settings?.SubscriptionKey ?? throw new ArgumentNullException("subscriptionKey");
var fromLanguage = DetermineLanguage("speaker") ?? throw new ArgumentNullException("fromLanguage");
Console.WriteLine();
var targetLanguage = DetermineLanguage("translator") ?? throw new ArgumentNullException("targetLanguage");
Console.WriteLine();

Console.Write("Record file name: ");
var filePath = Console.ReadLine() ?? throw new ArgumentNullException("filePath");
Console.WriteLine();

if (!Directory.Exists(directoryName))
{
    Directory.CreateDirectory(directoryName);
    Console.WriteLine("The directory was created successfully at {0}.", Directory.GetCreationTime(directoryName));
}

var translator = new Translator(endpointUrl, subscriptionKey, fromLanguage, targetLanguage);
var worker = new TranslationRecognizerWorker($"{directoryName}/{filePath}.txt");

try
{
    await translator.MultiLingualTranslation(worker);
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
