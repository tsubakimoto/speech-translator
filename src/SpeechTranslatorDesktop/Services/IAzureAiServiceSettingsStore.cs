namespace SpeechTranslatorDesktop.Services;

public interface IAzureAiServiceSettingsStore
{
    Task<AzureAiServiceSettings?> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(AzureAiServiceSettings settings, CancellationToken cancellationToken = default);
}
