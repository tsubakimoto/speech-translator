using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using System.Text;
namespace SpeechTranslatorWinUI.Core;
/// <summary>DPAPI encryption scoped to the current Windows account.</summary>
public sealed class DpapiSecretProtector : ISecretProtector
{
    /// <inheritdoc />
    public byte[] Protect(string value) => ProtectedData.Protect(Encoding.UTF8.GetBytes(value), null, DataProtectionScope.CurrentUser);
    /// <inheritdoc />
    public string Unprotect(byte[] value) => Encoding.UTF8.GetString(ProtectedData.Unprotect(value, null, DataProtectionScope.CurrentUser));
}
/// <summary>Independent SQLite store. A read error locks writes until explicit recovery.</summary>
public sealed class SettingsStore(string path, ISecretProtector protector) : ISettingsStore
{
    private bool _hasReadError;
    private bool _isLoaded;
    private SqliteConnection Open(bool create)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Mode = create ? SqliteOpenMode.ReadWriteCreate : SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
        try { connection.Open(); return connection; } catch { connection.Dispose(); throw; }
    }
    /// <inheritdoc />
    public Task<Settings?> LoadAsync() => Task.Run(() =>
    {
        try
        {
            if (!File.Exists(path)) { _isLoaded = true; return null; }
            using var connection = Open(false);
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT region, secret, source, target, folder FROM settings WHERE id=1";
            using var reader = command.ExecuteReader();
            _isLoaded = true;
            if (!reader.Read()) return null;
            var settings = new Settings(reader.GetString(0), protector.Unprotect((byte[])reader[1]), reader.GetString(2), reader.GetString(3), reader.IsDBNull(4) ? null : reader.GetString(4));
            if (settings.Source is not ("en-US" or "ja-JP") || settings.Target is not ("en-US" or "ja-JP")) throw new InvalidDataException("Unsupported stored language.");
            return settings;
        }
        catch { _hasReadError = true; throw; }
    });
    /// <inheritdoc />
    public Task SaveAsync(Settings settings) => Task.Run(() =>
    {
        if (!_isLoaded || _hasReadError) throw new InvalidOperationException("Settings must be loaded successfully or explicitly recovered before saving.");
        Save(settings);
    });
    private void Save(Settings settings)
    {
        var secret = protector.Protect(settings.Key);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        using var connection = Open(true);
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "CREATE TABLE IF NOT EXISTS settings(id INTEGER PRIMARY KEY CHECK(id=1), region TEXT NOT NULL, secret BLOB NOT NULL, source TEXT NOT NULL, target TEXT NOT NULL, folder TEXT);";
        command.ExecuteNonQuery();
        command.CommandText = "INSERT INTO settings VALUES(1,$region,$secret,$source,$target,$folder) ON CONFLICT(id) DO UPDATE SET region=$region,secret=$secret,source=$source,target=$target,folder=$folder";
        command.Parameters.AddWithValue("$region", settings.Region); command.Parameters.AddWithValue("$secret", secret);
        command.Parameters.AddWithValue("$source", settings.Source); command.Parameters.AddWithValue("$target", settings.Target); command.Parameters.AddWithValue("$folder", (object?)settings.Folder ?? DBNull.Value);
        command.ExecuteNonQuery(); transaction.Commit();
    }
    /// <inheritdoc />
    public Task<string> RecoverAsync(Settings settings) => Task.Run(() =>
    {
        var backup = path + ".recovery-" + Guid.NewGuid().ToString("N");
        // Move all SQLite companions too; originals are never overwritten or deleted.
        foreach (var suffix in new[] { "", "-wal", "-shm", "-journal" })
            if (File.Exists(path + suffix)) File.Move(path + suffix, backup + suffix);
        try { Save(settings); _hasReadError = false; _isLoaded = true; return backup; }
        catch { _hasReadError = true; throw; }
    });
}
