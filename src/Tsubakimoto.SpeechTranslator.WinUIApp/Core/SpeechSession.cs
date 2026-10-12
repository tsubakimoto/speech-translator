using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;
using Tsubakimoto.SpeechTranslator.Shared;
namespace Tsubakimoto.SpeechTranslator.WinUIApp.Core;
/// <summary>SDK resource owner. Unlike Shared's latched session stop, each completed failure permits retry.</summary>
public sealed class SpeechSession : ISpeechSession
{
    private TranslationRecognizer? _recognizer;
    private AudioConfig? _audio;
    private Worker? _worker;
    /// <inheritdoc />
    public event Action<string>? Status;
    /// <inheritdoc />
    public event Action<Translation>? Translated;
    /// <inheritdoc />
    public event Action<string>? Ended;
    /// <inheritdoc />
    public async Task StartAsync(Settings settings)
    {
        if (_recognizer is not null) throw new InvalidOperationException("A session already exists.");
        var config = SpeechTranslationConfig.FromEndpoint(new Uri($"wss://{settings.Region}.stt.speech.microsoft.com/speech/universal/v2"), settings.Key);
        config.SpeechRecognitionLanguage = settings.Source; config.AddTargetLanguage(settings.Target);
        _audio = AudioConfig.FromDefaultMicrophoneInput();
        _recognizer = new TranslationRecognizer(config, _audio);
        _worker = new Worker(this, settings.Target);
        _recognizer.Recognizing += Recognizing; _recognizer.Recognized += Recognized; _recognizer.Canceled += Canceled;
        _recognizer.SpeechStartDetected += SpeechStart; _recognizer.SpeechEndDetected += SpeechEnd;
        _recognizer.SessionStarted += SessionStart; _recognizer.SessionStopped += SessionStop;
        // Retain partially started resources on failure so the same stop policy releases them.
        await _recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);
    }
    /// <inheritdoc />
    public async Task StopAsync()
    {
        if (_recognizer is null) { _audio?.Dispose(); _audio = null; return; }
        await _recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
        _recognizer.Recognizing -= Recognizing; _recognizer.Recognized -= Recognized; _recognizer.Canceled -= Canceled;
        _recognizer.SpeechStartDetected -= SpeechStart; _recognizer.SpeechEndDetected -= SpeechEnd;
        _recognizer.SessionStarted -= SessionStart; _recognizer.SessionStopped -= SessionStop;
        _recognizer.Dispose(); _recognizer = null; _audio?.Dispose(); _audio = null; _worker = null;
    }
    private void Recognizing(object? s, TranslationRecognitionEventArgs e) => _worker?.OnRecognizing(e);
    private void Recognized(object? s, TranslationRecognitionEventArgs e) => _worker?.OnRecognized(e);
    private void Canceled(object? s, TranslationRecognitionCanceledEventArgs e) => _worker?.OnCanceled(e);
    private void SpeechStart(object? s, RecognitionEventArgs e) => _worker?.OnSpeechStartDetected(e);
    private void SpeechEnd(object? s, RecognitionEventArgs e) => _worker?.OnSpeechEndDetected(e);
    private void SessionStart(object? s, SessionEventArgs e) => _worker?.OnSessionStarted(e);
    private void SessionStop(object? s, SessionEventArgs e) => _worker?.OnSessionStopped(e);
    private sealed class Worker(SpeechSession owner, string target) : TranslationRecognizerWorkerBase
    {
        public override void OnRecognizing(TranslationRecognitionEventArgs e) => owner.Status?.Invoke("認識中");
        public override void OnRecognized(TranslationRecognitionEventArgs e)
        {
            if (e.Result.Reason == ResultReason.TranslatedSpeech)
            {
                var text = e.Result.Translations.TryGetValue(target, out var value) ? value : e.Result.Translations.Values.FirstOrDefault() ?? "";
                owner.Translated?.Invoke(new(e.Result.Text, text));
            }
            else owner.Status?.Invoke(e.Result.Reason == ResultReason.NoMatch ? "NOMATCH: Speech could not be recognized." : "認識のみ: " + e.Result.Text);
        }
        public override void OnCanceled(TranslationRecognitionCanceledEventArgs e) => owner.Ended?.Invoke(e.Reason == CancellationReason.Error ? "Cancel/Error: " + e.ErrorDetails : "Canceled: " + e.Reason);
        public override void OnSpeechStartDetected(RecognitionEventArgs e) => owner.Status?.Invoke("音声開始を検出");
        public override void OnSpeechEndDetected(RecognitionEventArgs e) => owner.Status?.Invoke("音声終了を検出");
        public override void OnSessionStarted(SessionEventArgs e) => owner.Status?.Invoke("セッション開始");
        public override void OnSessionStopped(SessionEventArgs e) => owner.Ended?.Invoke("セッション停止");
    }
}
