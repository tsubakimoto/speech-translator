using SpeechTranslatorDesktop.Models;
using SpeechTranslatorShared;

namespace SpeechTranslatorDesktop.Services;

public sealed class DesktopTranslationWorker : TranslationRecognizerWorkerBase, IDesktopTranslationWorker
{
    private readonly string _targetLanguage;
    private readonly string? _recordingFileName;
    private readonly IRecordingFileService _recordingFileService;

    public DesktopTranslationWorker(string targetLanguage, string? recordingFileName, IRecordingFileService recordingFileService)
    {
        if (string.IsNullOrWhiteSpace(targetLanguage))
        {
            throw new ArgumentException($"'{nameof(targetLanguage)}' を NULL または空にすることはできません。", nameof(targetLanguage));
        }

        _targetLanguage = targetLanguage;
        _recordingFileName = recordingFileName;
        _recordingFileService = recordingFileService ?? throw new ArgumentNullException(nameof(recordingFileService));
    }

    public TranslationRecognizerWorkerBase RecognizerWorker => this;

    public event EventHandler<string>? MessageLogged;

    public event EventHandler<WorkerStatusChangedEventArgs>? StatusChanged;

    public event EventHandler<TranslationLogItem>? TranslationLogged;

    public override void OnRecognizing(TranslationRecognitionEventArgs e)
    {
        RaiseStatusChanged(DesktopTranslationStatus.Recognizing, "認識中");
    }

    public override void OnRecognized(TranslationRecognitionEventArgs e)
    {
        var result = e.Result;

        if (result.Reason == ResultReason.TranslatedSpeech)
        {
            var translatedText = result.Translations.TryGetValue(_targetLanguage, out var value)
                ? value
                : result.Translations.Values.FirstOrDefault() ?? string.Empty;
            HandleTranslatedSpeech(result.Text, translatedText);
            return;
        }

        if (result.Reason == ResultReason.RecognizedSpeech)
        {
            RaiseStatusChanged(DesktopTranslationStatus.RecognizedSpeech, "認識のみ");
            MessageLogged?.Invoke(this, $"認識のみ: {result.Text}");
            return;
        }

        if (result.Reason == ResultReason.NoMatch)
        {
            RaiseStatusChanged(DesktopTranslationStatus.NoMatch, "NoMatch");
            MessageLogged?.Invoke(this, "NOMATCH: Speech could not be recognized.");
        }
    }

    public override void OnCanceled(TranslationRecognitionCanceledEventArgs e)
    {
        var message = e.Reason == CancellationReason.Error
            ? $"Cancel/Error: {e.ErrorDetails}"
            : $"Canceled: {e.Reason}";

        RaiseStatusChanged(DesktopTranslationStatus.Canceled, "Cancel/Error");
        MessageLogged?.Invoke(this, message);
    }

    public override void OnSpeechStartDetected(RecognitionEventArgs e)
    {
        RaiseStatusChanged(DesktopTranslationStatus.SpeechStartDetected, "音声開始を検出");
    }

    public override void OnSpeechEndDetected(RecognitionEventArgs e)
    {
        RaiseStatusChanged(DesktopTranslationStatus.SpeechEndDetected, "音声終了を検出");
    }

    public override void OnSessionStarted(SessionEventArgs e)
    {
        RaiseStatusChanged(DesktopTranslationStatus.SessionStarted, "セッション開始");
    }

    public override void OnSessionStopped(SessionEventArgs e)
    {
        RaiseStatusChanged(DesktopTranslationStatus.SessionStopped, "セッション停止");
    }

    internal void HandleTranslatedSpeech(string sourceText, string translatedText)
    {
        var translation = new TranslationLogItem(sourceText, translatedText);
        TranslationLogged?.Invoke(this, translation);
        MessageLogged?.Invoke(this, $"原文: {translation.SourceText}");
        MessageLogged?.Invoke(this, $"翻訳: {translation.TranslatedText}");

        try
        {
            _recordingFileService.AppendTranslation(_recordingFileName, translation.SourceText, translation.TranslatedText);
            RaiseStatusChanged(DesktopTranslationStatus.TranslatedSpeech, "翻訳成功");
        }
        catch (Exception ex)
        {
            RaiseStatusChanged(DesktopTranslationStatus.Error, $"記録ファイルの保存に失敗しました: {ex.Message}");
        }
    }

    private void RaiseStatusChanged(DesktopTranslationStatus status, string message)
    {
        StatusChanged?.Invoke(this, new WorkerStatusChangedEventArgs(status, message));
    }
}
