using System.Text.Json;

namespace SpeechTranslator.Desktop;

public interface IDesktopSettingsStore
{
    DesktopSettings Load();

    void Save(DesktopSettings settings);
}

public sealed class DesktopSettingsStore : IDesktopSettingsStore
{
    private readonly string _settingsPath;

    public DesktopSettingsStore(string? appDataPath = null)
    {
        var basePath = appDataPath ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _settingsPath = Path.Combine(basePath, "SpeechTranslator", "desktop-settings.json");
    }

    public DesktopSettings Load()
    {
        if (!File.Exists(_settingsPath))
        {
            return DesktopSettings.CreateDefault();
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<DesktopSettings>(json)?.Normalize() ?? DesktopSettings.CreateDefault();
        }
        catch (JsonException)
        {
            return DesktopSettings.CreateDefault();
        }
        catch (IOException)
        {
            return DesktopSettings.CreateDefault();
        }
        catch (UnauthorizedAccessException)
        {
            return DesktopSettings.CreateDefault();
        }
    }

    public void Save(DesktopSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_settingsPath, json);
    }
}
