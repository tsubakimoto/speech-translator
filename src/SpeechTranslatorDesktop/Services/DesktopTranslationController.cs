using SpeechTranslatorShared;

namespace SpeechTranslatorDesktop.Services;

public sealed class DesktopTranslationController : ITranslationController
{
    private readonly object _syncRoot = new();
    private readonly Func<SpeechCredentials, string, string, TranslationRecognizerWorkerBase, CancellationToken, Task<ITranslationSession>> _startSessionAsync;
    private ITranslationSession? _session;
    private ITranslationSession? _sessionBeingStopped;

    public DesktopTranslationController()
        : this(StartSessionAsync)
    {
    }

    internal DesktopTranslationController(Func<SpeechCredentials, string, string, TranslationRecognizerWorkerBase, CancellationToken, Task<ITranslationSession>> startSessionAsync)
    {
        _startSessionAsync = startSessionAsync ?? throw new ArgumentNullException(nameof(startSessionAsync));
    }

    public bool IsRunning
    {
        get
        {
            lock (_syncRoot)
            {
                return _session is not null;
            }
        }
    }

    public async Task StartAsync(SpeechCredentials credentials, string sourceLanguage, string targetLanguage, TranslationRecognizerWorkerBase worker, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        ArgumentNullException.ThrowIfNull(worker);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceLanguage);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetLanguage);

        lock (_syncRoot)
        {
            if (_session is not null)
            {
                throw new InvalidOperationException("Translation is already running.");
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        var session = await _startSessionAsync(credentials, sourceLanguage, targetLanguage, worker, cancellationToken).ConfigureAwait(false);

        lock (_syncRoot)
        {
            _session = session;
        }

        _ = ObserveCompletionAsync(session);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ITranslationSession? session;
        lock (_syncRoot)
        {
            session = _session;

            if (session is not null)
            {
                _sessionBeingStopped = session;
            }
        }

        if (session is null)
        {
            return;
        }

        try
        {
            await session.StopAsync().ConfigureAwait(false);
            await session.DisposeAsync().ConfigureAwait(false);

            lock (_syncRoot)
            {
                if (ReferenceEquals(_session, session))
                {
                    _session = null;
                }

                if (ReferenceEquals(_sessionBeingStopped, session))
                {
                    _sessionBeingStopped = null;
                }
            }
        }
        catch
        {
            lock (_syncRoot)
            {
                if (ReferenceEquals(_sessionBeingStopped, session))
                {
                    _sessionBeingStopped = null;
                }
            }

            throw;
        }
    }

    private async Task ObserveCompletionAsync(ITranslationSession session)
    {
        try
        {
            await session.Completion.ConfigureAwait(false);
        }
        finally
        {
            var shouldDispose = false;
            lock (_syncRoot)
            {
                if (ReferenceEquals(_session, session) && !ReferenceEquals(_sessionBeingStopped, session))
                {
                    _session = null;
                    shouldDispose = true;
                }
            }

            if (shouldDispose)
            {
                await session.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    private static Task<ITranslationSession> StartSessionAsync(SpeechCredentials credentials, string sourceLanguage, string targetLanguage, TranslationRecognizerWorkerBase worker, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var endpointUrl = new Uri($"wss://{credentials.Region}.stt.speech.microsoft.com/speech/universal/v2");
        var translator = new Translator(endpointUrl, credentials.Key, sourceLanguage, targetLanguage);
        return translator.StartTranslationAsync(worker);
    }
}
