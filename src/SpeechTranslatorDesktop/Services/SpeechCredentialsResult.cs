namespace SpeechTranslatorDesktop.Services;

public sealed record SpeechCredentialsResult(bool IsValid, SpeechCredentials? Credentials, string ErrorMessage)
{
    public static SpeechCredentialsResult Success(SpeechCredentials credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        return new SpeechCredentialsResult(true, credentials, string.Empty);
    }

    public static SpeechCredentialsResult Failure(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            throw new ArgumentException($"'{nameof(errorMessage)}' を NULL または空にすることはできません。", nameof(errorMessage));
        }

        return new SpeechCredentialsResult(false, null, errorMessage);
    }
}
