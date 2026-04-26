namespace SpeechTranslatorDesktop.Services;

public interface ISecretProtector
{
    byte[] Protect(string plaintext);

    string Unprotect(byte[] protectedData);
}
