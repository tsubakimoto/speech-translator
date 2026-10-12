namespace Tsubakimoto.SpeechTranslator.DesktopApp.Services;

public interface ISpeechCredentialsProvider
{
    SpeechCredentialsResult GetCredentials(string? preferredRegion = null, string? preferredKey = null);
}
