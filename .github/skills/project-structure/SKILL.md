---
name: project-structure
description: Explains the project structure of the speech-translator solution
---

# Speech Translator .NET Project Structure

```
speech-translator/
├── src/
│   ├── Tsubakimoto.SpeechTranslator.Shared/            # Shared source code for the application
│   ├── Tsubakimoto.SpeechTranslator.ConsoleApp/        # Console application for the speech translator
│   ├── Tsubakimoto.SpeechTranslator.DesktopApp/        # WPF desktop application
│   └── Tsubakimoto.SpeechTranslator.WinUIApp/          # WinUI application
└── tests/
    ├── Tsubakimoto.SpeechTranslator.Shared.Tests/
    ├── Tsubakimoto.SpeechTranslator.DesktopApp.Tests/
    └── Tsubakimoto.SpeechTranslator.WinUIApp.Tests/
```

## Main Folders

| Folder | Contents |
|--------|----------|
| `infra/` | Infrastructure as code and related configurations |
| `src/` | Source code projects |
| `tests/` | Test projects named `<Source-Code-Project>.Tests` |
