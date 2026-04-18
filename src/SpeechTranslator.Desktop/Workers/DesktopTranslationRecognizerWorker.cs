namespace SpeechTranslator.Desktop;

public interface ITranslationSessionViewModel
{
    void SetCurrentSourceText(string text);

    void SetCurrentTranslatedText(string text);

    void AddTranslation(TranslationEntry entry);

    void SetStatusMessage(string text);
}

public sealed class DesktopTranslationRecognizerWorker : TranslationRecognizerWorkerBase
{
    private readonly IUiDispatcher _dispatcher;
    private readonly ITranslationSessionViewModel _viewModel;
    private readonly IRecordingWriter? _recordingWriter;
    private readonly string _targetLanguage;

    public DesktopTranslationRecognizerWorker(IUiDispatcher dispatcher, ITranslationSessionViewModel viewModel, IRecordingWriter? recordingWriter, string targetLanguage)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _recordingWriter = recordingWriter;
        _targetLanguage = targetLanguage ?? throw new ArgumentNullException(nameof(targetLanguage));
    }

    public override void OnRecognizing(TranslationRecognitionEventArgs e)
    {
        var text = e.Result.Text ?? string.Empty;
        _dispatcher.Post(() => _viewModel.SetCurrentSourceText(text));
    }

    public override void OnRecognized(TranslationRecognitionEventArgs e)
    {
        var result = e.Result;
        var sourceText = result.Text ?? string.Empty;

        if (result.Reason == ResultReason.TranslatedSpeech)
        {
            var translatedText = result.Translations.TryGetValue(_targetLanguage, out var translation)
                ? translation
                : string.Empty;

            var entry = new TranslationEntry(DateTimeOffset.Now, sourceText, translatedText);

            _recordingWriter?.Write(entry);
            _dispatcher.Post(() =>
            {
                _viewModel.SetCurrentSourceText(sourceText);
                _viewModel.SetCurrentTranslatedText(translatedText);
                _viewModel.AddTranslation(entry);
            });
            return;
        }

        _dispatcher.Post(() =>
        {
            _viewModel.SetCurrentSourceText(sourceText);
            _viewModel.SetCurrentTranslatedText(string.Empty);
        });
    }

    public override void OnCanceled(TranslationRecognitionCanceledEventArgs e)
    {
        _dispatcher.Post(() => _viewModel.SetStatusMessage($"Canceled: {e.Reason}"));
    }

    public override void OnSpeechStartDetected(RecognitionEventArgs e)
    {
        _dispatcher.Post(() => _viewModel.SetStatusMessage("Speech detected."));
    }

    public override void OnSpeechEndDetected(RecognitionEventArgs e)
    {
        _dispatcher.Post(() => _viewModel.SetStatusMessage("Speech ended."));
    }

    public override void OnSessionStarted(SessionEventArgs e)
    {
        _dispatcher.Post(() => _viewModel.SetStatusMessage("Session started."));
    }

    public override void OnSessionStopped(SessionEventArgs e)
    {
        _dispatcher.Post(() => _viewModel.SetStatusMessage("Session stopped."));
    }
}
