# SpeechTranslatorWinUI

Packaged WinUI 3 counterpart of SpeechTranslatorDesktop. WPF and Shared remain unchanged; the app references Shared, never Desktop. Two pages (translation and settings), manual application composition, CommunityToolkit.Mvvm, and an independent SQLite/DPAPI store implement the accepted ADR constraints.

## Implementation choices

These are implementation choices, not new accepted ADRs: Windows 11 24H2 (build 26100) minimum; x64 only; stable .NET 10 SDK (local pin 10.0.401); .NET and Windows App SDK framework-dependent; no Native AOT. Exact NuGet versions: Windows App SDK 2.5.1, SDK BuildTools 10.0.28000.2705, WinApp/analyzer 0.7.1, MVVM Toolkit 8.4.2, SQLite/ProtectedData 10.0.12. Speech SDK 1.51.2 comes from Shared. The root SDK selection excludes previews while allowing installed stable .NET 10 feature bands.

The SDK adapter uses Shared's worker base, but owns recognizer/audio lifetime itself: Shared's session latches a stop request before completion, so it cannot safely retry a failed SDK stop. This does not change Shared or WPF behavior.

## Use

Install the signed MSIX only on a machine with the matching trusted signing certificate, .NET 10 x64 runtime and Windows App SDK 2.5 runtime dependencies. Installation, dependency provisioning and certificate trust require a separate explicit operator decision; building does not perform them. Set the default Windows microphone and permit microphone access. A valid Azure Speech key and region with translation support are required for actual recognition.

- On first launch, enter region/key in Settings and explicitly Save. Cancel/discard restores committed credentials. A failed save retains the draft. Leaving settings or closing with unsaved credentials offers save/discard/cancel.
- Settings live in `ApplicationData.Current.LocalFolder/speech-translator-winui.db` (under the package's per-user LocalState), independent of WPF. Key bytes use DPAPI CurrentUser; copying to a different Windows user is not supported. Region, language and absolute recording-folder path are not encrypted.
- `SPEECH_REGION` / `SPEECH_KEY` are a fallback only after a successful empty load. A database/read/decryption error never falls back or silently recreates the store. Recovery is explicit, confirmed, and retains the original database and SQLite sidecars beside it with unique `.recovery-<GUID>` names. Failed recovery preserves any already-retained originals. Recovery creates default preferences plus the entered credentials; it is not migration or import.
- Choose `en-US` / `ja-JP`; initial direction is English to Japanese. Language and folder selections save immediately and roll back on failure. Folder has no initial value; the desktop full-trust app persists the selected absolute path. No future-access token is needed. A folder can later become unavailable; start revalidates it when recording is requested.
- Filename starts blank on every launch. Blank means no recording and does not require a folder. Otherwise use a 1–200 character ASCII letter/digit/`-`/`_` stem, no path/extension, excluding Windows device names. The app appends to `<stem>.txt` in the selected folder: original text, translated text and a blank line, UTF-8 without BOM. Existing contents are not overwritten. No audio is recorded.
- Start freezes edits immediately. Settings can still be viewed. Stop ignores subsequent results, warns after ten seconds and continues waiting without blocking the UI. A completed stop failure keeps edits frozen and permits retry; pending stop requests share the same task. Recording-write failure stops translation. Close waits for stop/release and offers retry/wait, cancel close or exit anyway after failure/delay. Exit anyway cannot guarantee final recording/resource cleanup.
- Results and timestamped status logs autoscroll and remain unbounded for the lifetime of the process. A second WinUI instance forwards activation to the first; Console and WPF are unaffected.

No device selection, audio recording, history, export/import, background operation or WPF settings migration is added.

## Build and tests

From the repository root on Windows, with the stable SDK installed:

```powershell
dotnet restore speech-translator.slnx
dotnet build speech-translator.slnx --configuration Release
dotnet test --solution speech-translator.slnx --configuration Release --no-build
```

Focused core tests (no app installation, UI, credentials or microphone needed):

```powershell
dotnet test --project tests/SpeechTranslator.WinUI.Tests/SpeechTranslator.WinUI.Tests.csproj
```

CI builds the app and runs these tests through the solution. Existing Console/WPF publish and release archives remain the only release targets; CI does not package, sign, install or release WinUI.

## Local MSIX only

With WinApp CLI 0.7+, run from this directory. Put all signing material outside the repository:

```powershell
winapp cert generate --manifest ./Package.appxmanifest --output "$env:TEMP/speech-winui-dev.pfx"
winapp package ./SpeechTranslatorWinUI.csproj --arch x64 --cert "$env:TEMP/speech-winui-dev.pfx" --output "$env:TEMP/SpeechTranslatorWinUI-local.msix"
```

The scaffold identity `D65DF0BC-97EC-4C27-B2DB-D970D2459D60`, publisher `CN=AppPublisher`, version `1.0.0.0` is retained for local development only, not a Store/production identity. Generated certificates with the public default password are development-only. Never commit PFX/private keys/passwords or run `cert install` / development registration without explicit approval. This project is framework-dependent: MSIX is not a standalone/single-file executable.

The official `winapp new` wrapper in CLI 0.7.1 passes an obsolete `--dotnetVersion` option to the current template. Scaffolding used the same installed Microsoft `winui-mvvm` template directly with `dotnet new`, then corrected its x64/framework-dependent settings.

## Validation boundaries

The final local WinUI Release build succeeded with zero warnings/errors, and all 309 WinUI tests passed. Core tests cover settings persistence, real SQLite/DPAPI round-trips and failure/recovery paths, recording validation/output, deterministic MVVM lifecycle races, and close confirmation/timeout/retry behavior. The full solution Release run passed 365 of 366 tests: the unchanged WPF `StopAsync_WhenDisposeFails_KeepsControllerRunningUntilRetrySucceeds` test failed. The isolated unchanged HEAD WPF project passed ten subsequent Release reruns, and its full solution passed five reruns; the integrated failure remains unresolved rather than being counted as a successful regression run. WPF code/tests were not modified.

A final x64 local MSIX was generated and signed outside the repository. Its manifest and signature entry were inspected; the signer is `CN=AppPublisher`. Windows signature trust verification fails because the self-signed development certificate is not trusted. No certificate trust, application installation, or OS configuration changes were performed. CI explicitly installs SDK 10.0.401 alongside the current stable 10.0.x so the exact test SDK pin remains available.

Build/MSIX checks do not prove UI, native Speech SDK stop behavior or Azure translation works. Packaged UI launch requires approved installation/trust; live speech additionally requires credentials and a microphone. These are not replaced by mocks or an unpackaged launch.
