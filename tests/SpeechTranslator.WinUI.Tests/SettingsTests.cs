using SpeechTranslatorWinUI.Core;

namespace SpeechTranslator.WinUI.Tests;

/// <summary>Tests committed settings defaults and local credential syntax.</summary>
public sealed class SettingsTests
{
    /// <summary>Verifies all first-launch defaults independently.</summary>
    [Fact]
    public void Settings_Defaults_AreEnglishToJapaneseWithoutFolderOrCredentials()
    {
        // Arrange
        var settings = new Settings();

        // Act
        var hasCredentials = settings.HasCredentials;

        // Assert
        Assert.Equal("", settings.Region);
        Assert.Equal("", settings.Key);
        Assert.Equal("en-US", settings.Source);
        Assert.Equal("ja-JP", settings.Target);
        Assert.Null(settings.Folder);
        Assert.False(hasCredentials);
    }

    /// <summary>Accepts valid ASCII regions and nonwhitespace secrets without service verification.</summary>
    [Theory]
    [InlineData("japaneast", "key")]
    [InlineData("ab", "!@$%^&*()_+-=:;/'\"?.,")]
    [InlineData("a1-", "MiXeD123")]
    [InlineData("a-b0c9", "秘密🔑")]
    [InlineData("length62", "key")]
    [InlineData("length63", "key")]
    public void HasCredentials_ValidRegionAndKey_IsTrue(string region, string key)
    {
        // Arrange
        region = region switch { "length62" => new string('a', 62), "length63" => new string('a', 63), _ => region };
        var settings = new Settings(region, key);

        // Act
        var actual = settings.HasCredentials;

        // Assert
        Assert.True(actual);
    }

    /// <summary>Rejects each invalid region partition with an otherwise valid key.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("length64")]
    [InlineData("Ab")]
    [InlineData("aB")]
    [InlineData("1a")]
    [InlineData("-a")]
    [InlineData(" ab")]
    [InlineData("ab ")]
    [InlineData("a b")]
    [InlineData("a_b")]
    [InlineData("a/b")]
    [InlineData("a\nb")]
    [InlineData("a\rb")]
    [InlineData("a\tb")]
    [InlineData("aé")]
    [InlineData("日本")]
    [InlineData("ab\r\n")]
    public void HasCredentials_InvalidRegion_IsFalse(string region)
    {
        // Arrange
        region = region == "length64" ? new string('a', 64) : region;
        var settings = new Settings(region, "valid-key");

        // Act
        var actual = settings.HasCredentials;

        // Assert
        Assert.False(actual);
    }

    /// <summary>Requires the region grammar to consume the final line feed too.</summary>
    [Fact]
    public void HasCredentials_RegionWithFinalLineFeed_IsFalse()
    {
        // Arrange
        var settings = new Settings("ab\n", "valid-key");

        // Act
        var actual = settings.HasCredentials;

        // Assert
        Assert.False(actual);
    }

    /// <summary>Rejects empty keys and leading, interior or trailing whitespace.</summary>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n\u00a0")]
    [InlineData(" key")]
    [InlineData("key ")]
    [InlineData("ke y")]
    [InlineData("\tkey")]
    [InlineData("key\t")]
    [InlineData("ke\ty")]
    [InlineData("\rkey")]
    [InlineData("key\r")]
    [InlineData("ke\ry")]
    [InlineData("\nkey")]
    [InlineData("key\n")]
    [InlineData("ke\ny")]
    [InlineData("\u00a0key")]
    [InlineData("key\u00a0")]
    [InlineData("ke\u00a0y")]
    public void HasCredentials_BlankOrWhitespaceContainingKey_IsFalse(string key)
    {
        // Arrange
        var settings = new Settings("ab", key);

        // Act
        var actual = settings.HasCredentials;

        // Assert
        Assert.False(actual);
    }
}
