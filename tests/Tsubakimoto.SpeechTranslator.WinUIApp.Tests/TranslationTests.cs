using Tsubakimoto.SpeechTranslator.WinUIApp.Core;

namespace Tsubakimoto.SpeechTranslator.WinUIApp.Tests;

/// <summary>Tests translation payload preservation without normalizing user text.</summary>
public sealed class TranslationTests
{
    /// <summary>Preserves each independent source and target field exactly.</summary>
    [Theory]
    [InlineData("", "")]
    [InlineData("", "訳")]
    [InlineData("原文", "")]
    [InlineData("Hello 🌍", "こんにちは世界")]
    [InlineData("source\r\nline\n", "訳\n別\r\n")]
    public void Translation_PreservesEmptyUnicodeAndNewlineFields(string source, string target)
    {
        // Arrange
        var expectedSource = source;
        var expectedTarget = target;

        // Act
        var translation = new Translation(source, target);

        // Assert
        Assert.Equal(expectedSource, translation.SourceText);
        Assert.Equal(expectedTarget, translation.TranslatedText);
    }
}
