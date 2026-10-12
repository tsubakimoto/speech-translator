namespace Tsubakimoto.SpeechTranslator.DesktopApp.Services;

public interface ISecretProtector
{
    byte[] Protect(string plaintext);

    string Unprotect(byte[] protectedData);
}
