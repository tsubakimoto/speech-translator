namespace SpeechTranslator.Desktop;

public interface ITranslatorService
{
    bool IsRunning { get; }

    Task StartAsync(TranslationSessionOptions options, TranslationRecognizerWorkerBase worker, CancellationToken cancellationToken = default);

    Task StopAsync();
}

public sealed class TranslatorService : ITranslatorService
{
    private readonly object _gate = new();
    private CancellationTokenSource? _sessionCancellation;
    private Task? _sessionTask;

    public bool IsRunning
    {
        get
        {
            lock (_gate)
            {
                return _sessionTask is { IsCompleted: false };
            }
        }
    }

    public Task StartAsync(TranslationSessionOptions options, TranslationRecognizerWorkerBase worker, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(worker);

        if (IsRunning)
        {
            throw new InvalidOperationException("A translation session is already running.");
        }

        var endpointUrl = new Uri($"wss://{options.Region}.stt.speech.microsoft.com/speech/universal/v2");
        var translator = new Translator(endpointUrl, options.SubscriptionKey, options.SourceLanguage, options.TargetLanguage, options.MicrophoneDeviceName);

        var sessionCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var sessionTask = translator.MultiLingualTranslation(worker, sessionCancellation.Token);

        lock (_gate)
        {
            _sessionCancellation = sessionCancellation;
            _sessionTask = sessionTask;
        }

        sessionTask.ContinueWith(t => CleanupSession(t), TaskScheduler.Default);
        return sessionTask;
    }

    public async Task StopAsync()
    {
        CancellationTokenSource? sessionCancellation;
        Task? sessionTask;
        lock (_gate)
        {
            sessionCancellation = _sessionCancellation;
            sessionTask = _sessionTask;
        }

        if (sessionCancellation is null)
        {
            return;
        }

        sessionCancellation.Cancel();

        if (sessionTask is not null)
        {
            try
            {
                await sessionTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        CleanupSession(sessionTask);
    }

    private void CleanupSession(Task? expectedTask = null)
    {
        lock (_gate)
        {
            if (expectedTask is not null && !ReferenceEquals(_sessionTask, expectedTask))
            {
                return;
            }

            _sessionCancellation?.Dispose();
            _sessionCancellation = null;
            _sessionTask = null;
        }
    }
}
