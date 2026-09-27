---
name: build-and-test
description: How to build and test .NET projects in the Speech Translator repository. Use this when verifying or testing changes.
---

- See `../project-structure/SKILL.md` for project structure details.
- Capture `dotnet` command output in a temporary log as required by `AGENTS.md`.

## Build, Test, and Lint Commands

Run from the repository root:

```bash
dotnet restore speech-translator.slnx --tl:off
dotnet build speech-translator.slnx --no-restore --tl:off
dotnet format speech-translator.slnx

# Run all unit tests (the desktop project requires Windows)
dotnet test --solution speech-translator.slnx --no-build --no-restore

# Build a single source project or test just the shared project
dotnet build src/SpeechTranslatorDesktop/SpeechTranslatorDesktop.csproj --tl:off
dotnet test --project tests/Shared.Tests/SpeechTranslatorShared.Tests.csproj --no-build --no-restore
```

Use `--tl:off` when building to avoid flickering. Run `dotnet restore` after changing package versions or project references; otherwise `--no-restore` avoids repeating the restore.

### xUnit v3 test runner

The tests use xUnit v3 with [Microsoft Testing Platform](https://learn.microsoft.com/dotnet/core/tools/dotnet-test-mtp). The repository's `global.json` selects the MTP runner, so `dotnet test`, `dotnet test --solution speech-translator.slnx`, and `dotnet test --project <test-project>` run the test projects. Use `--project` for a single project; do not use the legacy positional `dotnet test <project>` command.

To select a test or generate a TRX report through the xUnit executable, pass runner arguments after `--`:

```bash
dotnet run --project tests/Shared.Tests/SpeechTranslatorShared.Tests.csproj --no-build --no-restore -- -method "*TranslatorTest*"
dotnet run --project tests/Shared.Tests/SpeechTranslatorShared.Tests.csproj --no-build --no-restore -- -result-trx results.trx
```

For all supported options, use `dotnet run --project tests/Shared.Tests/SpeechTranslatorShared.Tests.csproj --no-build --no-restore -- -?`.