---
name: build-and-test
description: How to build and test .NET projects in the Speech Translator repository. Use this when verifying or testing changes.
---

- Only **UnitTest** projects need to be run locally; IntegrationTests require external dependencies.
- See `../project-structure/SKILL.md` for project structure details.

## Build, Test, and Lint Commands

```bash
# From dotnet/ directory
dotnet restore --tl:off   # Restore dependencies for all projects
dotnet build --tl:off     # Build all projects
dotnet test               # Run all tests
dotnet format             # Auto-fix formatting for all projects

# Build/test/format a specific project (preferred for isolated/internal changes)
dotnet build src/SpeechTranslatorDesktop --tl:off
dotnet test --project tests/SpeechTranslatorDesktop.Tests
dotnet format src/SpeechTranslatorDesktop

# Run a single test
# Replace the filter values with the appropriate assembly, namespace, class, and method names for the test you want to run and use * as a wildcard elsewhere, e.g. "/*/*/HttpClientTests/GetAsync_ReturnsSuccessStatusCode"
# Use `--ignore-exit-code 8` to avoid failing the build when no tests are found for some projects
dotnet test --filter-query "/<assemblyFilter>/<namespaceFilter>/<classFilter>/<methodFilter>" --ignore-exit-code 8

# Run unit tests only
# Use `--ignore-exit-code 8` to avoid failing the build when no tests are found for integration test projects
dotnet test --filter-query "/*UnitTests*/*/*/*" --ignore-exit-code 8
```

Use `--tl:off` when building to avoid flickering when running commands in the agent.

## Speeding Up Builds and Testing

Example: Building a single code project for all target frameworks

```bash
dotnet build ./src/SpeechTranslatorDesktop
```

Example: Building a single code project for just .NET 10.

```bash
dotnet build ./src/SpeechTranslatorDesktop -f net10.0
```

Example: Running tests for a single project using .NET 10.

```bash
dotnet test --project ./tests/SpeechTranslatorDesktop.Tests -f net10.0
```

<!--
Example: Running a single test in a specific project using .NET 10.
Provide the full namespace, class name, and method name for the test you want to run:

```bash
dotnet test --project ./tests/SpeechTranslatorDesktop.Tests -f net10.0 --filter-query "..."
```
-->

### Package Restore tip

`dotnet build` will try and restore packages for all projects on each build, which can be slow.
Unless packages have been changed, or it's the first time building the solution, add `--no-restore` to the build command to skip this step and speed up builds.

Just remember to run `dotnet restore` after pulling changes, making changes to project references, or when building for the first time.

### Microsoft Testing Platform (MTP)

Tests use the [Microsoft Testing Platform](https://learn.microsoft.com/dotnet/core/testing/unit-testing-platform-intro) via xUnit v3. Key differences from the legacy VSTest runner:

- **`dotnet test` requires `--project`** to specify a test project directly (positional arguments are no longer supported).
- **Test output** uses the MTP format (e.g., `[✓112/x0/↓0]` progress and `Test run summary: Passed!`).
- **TRX reports** use `--report-xunit-trx` instead of `--logger trx`.
- **Code coverage** uses `Microsoft.Testing.Extensions.CodeCoverage` with `--coverage --coverage-output-format cobertura`.
- **Running a test project directly** is supported via `dotnet run --project <test-project>`. This bypasses the `dotnet test` infrastructure and runs the test executable directly with the MTP command line.

- **Running tests across the solution** with a filter may cause some projects to match zero tests, which MTP treats as a failure (exit code 8). Use `--ignore-exit-code 8` to suppress this:

```bash
# Run all unit tests across the solution, ignoring projects with no matching tests
dotnet test --solution ./speech-translator.slnx --no-build -f net10.0 --ignore-exit-code 8
```

- **Running tests with `--solution` for a specific TFM** requires all projects in the solution to support that TFM. Not all projects target every framework (e.g., some are `net10.0`-only).

```bash
# Run tests via dotnet test (uses MTP under the hood)
dotnet test --project ./tests/SpeechTranslatorDesktop.Tests -f net10.0

# Run tests with code coverage (Cobertura format)
dotnet test --project ./tests/SpeechTranslatorDesktop.Tests -f net10.0 --coverage --coverage-output-format cobertura --coverage-settings ./tests/coverage.runsettings

# Run tests directly via dotnet run (MTP native command line)
dotnet run --project ./tests/SpeechTranslatorDesktop.Tests -f net10.0

# Show MTP command line help
dotnet run --project ./tests/SpeechTranslatorDesktop.Tests -f net10.0 -- -?
```