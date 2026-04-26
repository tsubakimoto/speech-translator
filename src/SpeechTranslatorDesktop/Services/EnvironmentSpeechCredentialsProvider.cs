namespace SpeechTranslatorDesktop.Services;

public sealed class EnvironmentSpeechCredentialsProvider : ISpeechCredentialsProvider
{
    private readonly Func<string, string?> _environmentReader;

    public EnvironmentSpeechCredentialsProvider(Func<string, string?>? environmentReader = null)
    {
        _environmentReader = environmentReader ?? Environment.GetEnvironmentVariable;
    }

    public SpeechCredentialsResult GetCredentials(string? preferredRegion = null, string? preferredKey = null)
    {
        var region = string.IsNullOrWhiteSpace(preferredRegion) ? _environmentReader("SPEECH_REGION") : preferredRegion.Trim();
        var key = string.IsNullOrWhiteSpace(preferredKey) ? _environmentReader("SPEECH_KEY") : preferredKey.Trim();
        var missingVariables = new List<string>();

        if (string.IsNullOrWhiteSpace(region))
        {
            missingVariables.Add("SPEECH_REGION");
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            missingVariables.Add("SPEECH_KEY");
        }

        if (missingVariables.Count > 0)
        {
            return SpeechCredentialsResult.Failure($"Azure AI Service の認証情報が未設定です。設定画面で保存するか、環境変数を設定してください: {string.Join(", ", missingVariables)}");
        }

        var normalizedRegion = region!.Trim();
        var normalizedKey = key!.Trim();

        return SpeechCredentialsResult.Success(new SpeechCredentials(normalizedRegion, normalizedKey));
    }
}
