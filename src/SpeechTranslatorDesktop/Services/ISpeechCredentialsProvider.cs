namespace SpeechTranslatorDesktop.Services;

public interface ISpeechCredentialsProvider
{
    SpeechCredentialsResult GetCredentials();
}
