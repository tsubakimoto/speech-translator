using Tsubakimoto.SpeechTranslator.DesktopApp.Services;

namespace Tsubakimoto.SpeechTranslator.DesktopApp.Tests;

public class EnvironmentSpeechCredentialsProviderTests
{
    [Fact]
    public void GetCredentials_BothValuesPresent_ReturnsCredentials()
    {
        // Arrange
        var provider = new EnvironmentSpeechCredentialsProvider(name => name switch
        {
            "SPEECH_REGION" => "japaneast",
            "SPEECH_KEY" => "test-key",
            _ => null
        });

        // Act
        var result = provider.GetCredentials();

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(result.Credentials);
        Assert.Equal("japaneast", result.Credentials!.Region);
        Assert.Equal("test-key", result.Credentials.Key);
    }

    [Fact]
    public void GetCredentials_PrefersConfiguredValuesOverEnvironmentVariables()
    {
        // Arrange
        var provider = new EnvironmentSpeechCredentialsProvider(name => name switch
        {
            "SPEECH_REGION" => "env-region",
            "SPEECH_KEY" => "env-key",
            _ => null
        });

        // Act
        var result = provider.GetCredentials("configured-region", "configured-key");

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(result.Credentials);
        Assert.Equal("configured-region", result.Credentials!.Region);
        Assert.Equal("configured-key", result.Credentials.Key);
    }

    [Fact]
    public void GetCredentials_WhenConfiguredValuesMissing_FallsBackToEnvironmentVariables()
    {
        // Arrange
        var provider = new EnvironmentSpeechCredentialsProvider(name => name switch
        {
            "SPEECH_REGION" => "env-region",
            "SPEECH_KEY" => "env-key",
            _ => null
        });

        // Act
        var result = provider.GetCredentials("", null);

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(result.Credentials);
        Assert.Equal("env-region", result.Credentials!.Region);
        Assert.Equal("env-key", result.Credentials.Key);
    }

    [Theory]
    [InlineData(null, "test-key", "SPEECH_REGION")]
    [InlineData("japaneast", null, "SPEECH_KEY")]
    [InlineData(null, null, "SPEECH_REGION, SPEECH_KEY")]
    public void GetCredentials_MissingValues_ReturnsError(string? region, string? key, string expected)
    {
        // Arrange
        var provider = new EnvironmentSpeechCredentialsProvider(name => name switch
        {
            "SPEECH_REGION" => region,
            "SPEECH_KEY" => key,
            _ => null
        });

        // Act
        var result = provider.GetCredentials();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(expected, result.ErrorMessage);
    }
}