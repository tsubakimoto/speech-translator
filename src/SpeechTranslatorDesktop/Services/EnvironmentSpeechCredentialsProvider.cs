namespace SpeechTranslatorDesktop.Services;

public sealed class EnvironmentSpeechCredentialsProvider : ISpeechCredentialsProvider
{
    private readonly Func<string, string?> _environmentReader;

    public EnvironmentSpeechCredentialsProvider(Func<string, string?>? environmentReader = null)
    {
        _environmentReader = environmentReader ?? Environment.GetEnvironmentVariable;
    }

    public SpeechCredentialsResult GetCredentials()
    {
        var region = _environmentReader("SPEECH_REGION");
        var key = _environmentReader("SPEECH_KEY");
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
            return SpeechCredentialsResult.Failure($"Required environment variables are missing: {string.Join(", ", missingVariables)}");
        }

        return SpeechCredentialsResult.Success(new SpeechCredentials(region!, key!));
    }
}
