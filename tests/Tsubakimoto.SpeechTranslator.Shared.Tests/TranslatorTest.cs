using Tsubakimoto.SpeechTranslator.Shared;

namespace Tsubakimoto.SpeechTranslator.Shared.Tests;

public class TranslatorTest
{
    [Fact]
    public void Constructor1()
    {
        // Arrange
        // Act
        Translator actual = CreateTranslator();

        // Assert
        Assert.NotNull(actual);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor1_InvalidSubscriptionKey(string subscriptionKey)
    {
        // Arrange
        // Act
        Action action = () => new Translator(
            new Uri("https://example.com"),
            subscriptionKey,
            "en-US",
            "ja-JP");

        // Assert
        Assert.ThrowsAny<ArgumentException>(action);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor1_InvalidRecognitionLanguage(string recognitionLanguage)
    {
        // Arrange
        // Act
        Action action = () => new Translator(
            new Uri("https://example.com"),
            "test",
            recognitionLanguage,
            "ja-JP");

        // Assert
        Assert.ThrowsAny<ArgumentException>(action);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor1_InvalidTargetLanguage(string targetLanguage)
    {
        // Arrange
        // Act
        Action action = () => new Translator(
            new Uri("https://example.com"),
            "test",
            "en-US",
            targetLanguage);

        // Assert
        Assert.ThrowsAny<ArgumentException>(action);
    }

    [Fact]
    public async Task MultiLingualTranslation_NullWorkerAsync()
    {
        // Arrange
        var translator = CreateTranslator();
        // Act
        Func<Task> action = () => translator.MultiLingualTranslation(null!);

        // Assert
        await Assert.ThrowsAnyAsync<ArgumentNullException>(action);
    }

    [Fact]
    public async Task StartTranslationAsync_NullWorkerAsync()
    {
        // Arrange
        var translator = CreateTranslator();
        // Act
        Func<Task> action = () => translator.StartTranslationAsync(null!);

        // Assert
        await Assert.ThrowsAnyAsync<ArgumentNullException>(action);
    }

    private static Translator CreateTranslator()
    {
        return new Translator(
            new Uri("https://example.com"),
            "test",
            "en-US",
            "ja-JP");
    }
}