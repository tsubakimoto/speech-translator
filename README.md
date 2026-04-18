# Speech Translator
This app is speech translator and recorder using [Azure AI Speech](https://azure.microsoft.com/en-us/products/ai-services/ai-speech).

## Prerequisites

- [.NET 10.0 SDK](https://dot.net/download)

## How to use

### Console app

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

### Desktop app (WPF)

Desktop app reads Azure AI Speech credentials from environment variables only.

1. Create Azure AI Speech resource. ([Bicep](./infra/main.bicep))
2. Set the microphone device for translation as the default input device.
3. Set environment variables in PowerShell.
   ```powershell
   $env:SPEECH_REGION="japaneast"
   $env:SPEECH_KEY="your-speech-key"
   ```
4. Run the desktop project.
   ```powershell
   dotnet run --project src/SpeechTranslatorDesktop
   ```
5. Select the speaker language and target language.
6. Optionally enter a simple recording file name stem (letters/numbers/`-`/`_`, no path or extension). If empty, no file is saved.
7. Click `開始` to start and `停止` to stop.

Recording files are saved as UTF-8 text files under `recordings/` relative to the desktop app executable directory.

## References

- https://learn.microsoft.com/en-us/azure/ai-services/speech-service/language-identification
- https://learn.microsoft.com/en-us/azure/ai-services/speech-service/how-to-translate-speech

## License

[see LICENSE](./LICENSE)
