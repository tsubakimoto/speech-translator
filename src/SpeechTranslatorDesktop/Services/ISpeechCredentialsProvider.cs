namespace SpeechTranslatorDesktop.Services;

public interface ISpeechCredentialsProvider
{
    SpeechCredentialsResult GetCredentials(string? preferredRegion = null, string? preferredKey = null);
}
