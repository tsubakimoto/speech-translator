namespace SpeechTranslator.Desktop.Tests;

public class DesktopSettingsStoreTests
{
    [Fact]
    public void Load_ReturnsDefaultsForCorruptedJson()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "SpeechTranslator"));
        File.WriteAllText(Path.Combine(root, "SpeechTranslator", "desktop-settings.json"), "not-json");

        var store = new DesktopSettingsStore(root);

        var settings = store.Load();

        settings.Should().Be(DesktopSettings.CreateDefault());
    }

    [Fact]
    public void Load_ReturnsDefaultsForTruncatedFile()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "SpeechTranslator"));
        File.WriteAllText(Path.Combine(root, "SpeechTranslator", "desktop-settings.json"), "{\"Region\":\"japaneast\"");

        var store = new DesktopSettingsStore(root);

        var settings = store.Load();

        settings.Should().Be(DesktopSettings.CreateDefault());
    }

    [Fact]
    public void Load_NormalizesNullSettingsValues()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "SpeechTranslator"));
        File.WriteAllText(
            Path.Combine(root, "SpeechTranslator", "desktop-settings.json"),
            """{"Region":null,"SourceLanguage":"fr-FR","TargetLanguage":"de-DE","RecordingName":null}""");

        var store = new DesktopSettingsStore(root);

        var settings = store.Load();

        settings.Should().Be(new DesktopSettings(string.Empty, "fr-FR", "de-DE", string.Empty));
    }

    [Fact]
    public void Save_PersistsDesktopSettingsWithoutSubscriptionKey()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var store = new DesktopSettingsStore(root);
        var settings = new DesktopSettings(
            "japaneast",
            "en-US",
            "ja-JP",
            "demo",
            "C:\\Users\\test\\Desktop\\captures",
            "USB Mic");

        store.Save(settings);

        var json = File.ReadAllText(Path.Combine(root, "SpeechTranslator", "desktop-settings.json"));
        json.Should().Contain("\"RecordingDirectory\"");
        json.Should().Contain("\"MicrophoneDeviceName\"");
        json.Should().NotContain("SubscriptionKey");
    }
}
