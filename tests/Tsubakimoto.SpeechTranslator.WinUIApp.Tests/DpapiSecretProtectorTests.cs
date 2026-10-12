using System.Security.Cryptography;
using System.Text;
using Tsubakimoto.SpeechTranslator.WinUIApp.Core;

namespace Tsubakimoto.SpeechTranslator.WinUIApp.Tests;

/// <summary>Exercises the Windows current-user DPAPI boundary without external resources.</summary>
public sealed class DpapiSecretProtectorTests
{
    /// <summary>Preserves empty, ASCII, Unicode and multiline secrets through real protection.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("Key-123_!'")]
    [InlineData("秘密🔑é")]
    [InlineData("first\r\n第二\nlast")]
    public void Dpapi_UnicodeAndEmptySecret_RoundTrips(string secret)
    {
        // Arrange
        var protector = new DpapiSecretProtector();

        // Act
        var cipher = protector.Protect(secret);
        var restored = protector.Unprotect(cipher);

        // Assert
        Assert.NotEmpty(cipher);
        Assert.False(cipher.SequenceEqual(Encoding.UTF8.GetBytes(secret)));
        Assert.Equal(secret, restored);
        Assert.Equal(secret, Encoding.UTF8.GetString(ProtectedData.Unprotect(cipher, null, DataProtectionScope.CurrentUser)));
        var independentlyProtected = ProtectedData.Protect(Encoding.UTF8.GetBytes(secret), null, DataProtectionScope.CurrentUser);
        Assert.Equal(secret, protector.Unprotect(independentlyProtected));
    }

    /// <summary>Rejects empty and truncated non-DPAPI ciphertext rather than returning a key.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Dpapi_MalformedCipher_ThrowsCryptographicException(bool truncated)
    {
        // Arrange
        var protector = new DpapiSecretProtector();
        byte[] cipher = truncated ? [0x01, 0x00, 0x00, 0x00, 0xD0] : [];

        // Act & Assert
        Assert.Throws<CryptographicException>(() => protector.Unprotect(cipher));
    }
}
