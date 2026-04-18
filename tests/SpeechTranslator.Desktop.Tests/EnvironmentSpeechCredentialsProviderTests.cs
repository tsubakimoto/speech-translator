using SpeechTranslatorDesktop.Services;

namespace SpeechTranslator.Desktop.Tests;

public class EnvironmentSpeechCredentialsProviderTests
{
    [Fact]
    public void GetCredentials_BothValuesPresent_ReturnsCredentials()
    {
        var provider = new EnvironmentSpeechCredentialsProvider(name => name switch
        {
            "SPEECH_REGION" => "japaneast",
            "SPEECH_KEY" => "test-key",
            _ => null
        });

        var result = provider.GetCredentials();

        result.IsValid.Should().BeTrue();
        result.Credentials.Should().NotBeNull();
        result.Credentials!.Region.Should().Be("japaneast");
        result.Credentials.Key.Should().Be("test-key");
    }

    [Theory]
    [InlineData(null, "test-key", "SPEECH_REGION")]
    [InlineData("japaneast", null, "SPEECH_KEY")]
    [InlineData(null, null, "SPEECH_REGION, SPEECH_KEY")]
    public void GetCredentials_MissingValues_ReturnsError(string? region, string? key, string expected)
    {
        var provider = new EnvironmentSpeechCredentialsProvider(name => name switch
        {
            "SPEECH_REGION" => region,
            "SPEECH_KEY" => key,
            _ => null
        });

        var result = provider.GetCredentials();

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain(expected);
    }
}
