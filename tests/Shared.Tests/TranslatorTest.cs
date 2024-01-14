namespace SpeechTranslatorShared.Tests;

public class TranslatorTest
{
    [Fact]
    public void Constructor1()
    {
        Translator actual = CreateTranslator();

        actual.Should().NotBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor1_InvalidSubscriptionKey(string subscriptionKey)
    {
        Action action = () => new Translator(
            new Uri("https://example.com"),
            subscriptionKey,
            "en-US",
            "ja-JP");

        action.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor1_InvalidRecognitionLanguage(string recognitionLanguage)
    {
        Action action = () => new Translator(
            new Uri("https://example.com"),
            "test",
            recognitionLanguage,
            "ja-JP");

        action.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor1_InvalidTargetLanguage(string targetLanguage)
    {
        Action action = () => new Translator(
            new Uri("https://example.com"),
            "test",
            "en-US",
            targetLanguage);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MultiLingualTranslation_NullWorker()
    {
        var translator = CreateTranslator();
        var action = () => translator.MultiLingualTranslation(null);

        action.Should().ThrowAsync<ArgumentNullException>();
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