namespace SpeechTranslator.Desktop;

public sealed record DesktopSettings(
    string Region,
    string SourceLanguage,
    string TargetLanguage,
    string RecordingName,
    string RecordingDirectory = "",
    string MicrophoneDeviceName = "")
{
    public static DesktopSettings CreateDefault() => new(string.Empty, "en-US", "ja-JP", string.Empty, string.Empty, string.Empty);

    public DesktopSettings Normalize()
    {
        var defaults = CreateDefault();
        return new DesktopSettings(
            (Region ?? defaults.Region).Trim(),
            (SourceLanguage ?? defaults.SourceLanguage).Trim(),
            (TargetLanguage ?? defaults.TargetLanguage).Trim(),
            (RecordingName ?? defaults.RecordingName).Trim(),
            (RecordingDirectory ?? defaults.RecordingDirectory).Trim(),
            (MicrophoneDeviceName ?? defaults.MicrophoneDeviceName).Trim());
    }
}
