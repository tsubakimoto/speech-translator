namespace SpeechTranslatorDesktop.Services;

public sealed class WorkerStatusChangedEventArgs : EventArgs
{
    public WorkerStatusChangedEventArgs(DesktopTranslationStatus status, string message)
    {
        Status = status;
        Message = message ?? string.Empty;
    }

    public DesktopTranslationStatus Status { get; }

    public string Message { get; }
}
