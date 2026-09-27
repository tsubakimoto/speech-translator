---
name: project-structure
description: Explains the project structure of the speech-translator solution
---

# Speech Translator .NET Project Structure

```
speech-translator/
├── src/
│   ├── Shared/                      # Shared source code for the application
│   ├── SpeechTranslatorConsole/     # Console application for the speech translator
│   └── SpeechTranslatorDesktop/     # Desktop application for the speech translator
└── tests/                           # Unit and integration tests
```

## Main Folders

| Folder | Contents |
|--------|----------|
| `infra/` | Infrastructure as code and related configurations |
| `src/` | Source code projects |
| `tests/` | Test projects — named `<Source-Code-Project>.Tests` |
