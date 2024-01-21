# Speech Translator
This app is speech translator and recorder using [Azure AI Speech](https://azure.microsoft.com/en-us/products/ai-services/ai-speech).

## Prerequisites

- [.NET 8.0 SDK](https://dot.net/download)

## How to use

1. Create Azure AI Speech resource. ([Bicep](./infra/main.bicep))
2. Copy `Subscription Key` and `Region` from Azure Portal.
3. Clone this repository.
4. Create `src/SpeechTranslatorConsole/appsettings.Development.json`.
5. Setup `appsettings.Development.json` like below.
    ```json
    {
        "Settings": {
            "Region": "<Region>",
            "SubscriptionKey": "<Subscription Key>"
        }
    }
    ```
6. Set the microphone device for translation as the default input device.
7. Run `SpeechTranslatorConsole` project. (`dotnet run --project src/SpeechTranslatorConsole`)
8. Type file name in console.

## References

- https://learn.microsoft.com/en-us/azure/ai-services/speech-service/language-identification
- https://learn.microsoft.com/en-us/azure/ai-services/speech-service/how-to-translate-speech

## License

[see LICENSE](./LICENSE)
