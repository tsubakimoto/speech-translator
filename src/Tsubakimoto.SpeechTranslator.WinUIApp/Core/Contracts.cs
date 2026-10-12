using System.Text.RegularExpressions;
namespace Tsubakimoto.SpeechTranslator.WinUIApp.Core;
/// <summary>Committed credentials and immediately persisted preferences.</summary>
public sealed record Settings(string Region = "", string Key = "", string Source = "en-US", string Target = "ja-JP", string? Folder = null)
{
    /// <summary>Whether credentials have valid local syntax (not service verification).</summary>
    public bool HasCredentials => Regex.IsMatch(Region, "\\A[a-z][a-z0-9-]{1,62}\\z") && !string.IsNullOrWhiteSpace(Key) && !Key.Any(char.IsWhiteSpace);
}
/// <summary>Settings persistence boundary.</summary>
public interface ISettingsStore
{
    /// <summary>Loads without mutating existing databases.</summary>
    Task<Settings?> LoadAsync();
    /// <summary>Atomically saves committed settings.</summary>
    Task SaveAsync(Settings settings);
    /// <summary>Explicitly retains the original database before recreating settings.</summary>
    Task<string> RecoverAsync(Settings settings);
}
/// <summary>Protects locally persisted secrets.</summary>
public interface ISecretProtector
{
    /// <summary>Encrypts a secret.</summary>
    byte[] Protect(string value);
    /// <summary>Decrypts a secret.</summary>
    string Unprotect(byte[] value);
}
/// <summary>Translation result displayed in the session log.</summary>
public sealed record Translation(string SourceText, string TranslatedText);
/// <summary>Speech lifecycle boundary with retryable stop.</summary>
public interface ISpeechSession
{
    /// <summary>Reports recognition and session activity.</summary>
    event Action<string>? Status;
    /// <summary>Reports a completed translation.</summary>
    event Action<Translation>? Translated;
    /// <summary>Reports SDK cancellation or fatal errors.</summary>
    event Action<string>? Ended;
    /// <summary>Starts microphone recognition.</summary>
    Task StartAsync(Settings settings);
    /// <summary>Stops and releases resources; may be retried after failure.</summary>
    Task StopAsync();
}
/// <summary>Recording filesystem boundary.</summary>
public interface IRecordingWriter
{
    /// <summary>Validates an optional recording destination before starting.</summary>
    string? Validate(string? folder, string filename);
    /// <summary>Appends one original/translation pair.</summary>
    void Append(string path, Translation translation);
}
