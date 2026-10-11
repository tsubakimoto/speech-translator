using Microsoft.Data.Sqlite;
using SpeechTranslatorWinUI.Core;

namespace SpeechTranslator.WinUI.Tests;

internal static class SqliteTestDatabase
{
    internal static readonly byte[] Payload = [0x91, 0x02, 0xFE, 0x38];
    internal static readonly string[] Suffixes = ["", "-wal", "-shm", "-journal"];

    internal static SqliteConnection Open(string path, SqliteOpenMode mode = SqliteOpenMode.ReadWriteCreate)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = mode,
            Pooling = false,
            DefaultTimeout = 1,
        }.ToString());
        connection.Open();
        return connection;
    }

    internal static void Create(string path, Settings? settings = null, int id = 1, object? secret = null)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var connection = Open(path);
        using var command = connection.CreateCommand();
        // Deliberately permissive schema permits malformed persisted-data fixtures.
        command.CommandText = "CREATE TABLE settings(id INTEGER PRIMARY KEY, region TEXT, secret BLOB, source TEXT, target TEXT, folder TEXT)";
        command.ExecuteNonQuery();
        if (settings is null)
            return;
        command.CommandText = "INSERT INTO settings VALUES($id,$region,$secret,$source,$target,$folder)";
        command.Parameters.AddWithValue("$id", id);
        command.Parameters.AddWithValue("$region", settings.Region);
        command.Parameters.AddWithValue("$secret", secret ?? Payload);
        command.Parameters.AddWithValue("$source", settings.Source);
        command.Parameters.AddWithValue("$target", settings.Target);
        command.Parameters.AddWithValue("$folder", (object?)settings.Folder ?? DBNull.Value);
        command.ExecuteNonQuery();
    }

    internal static void Execute(string path, string sql)
    {
        using var connection = Open(path);
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    internal static void AssertRow(string path, Settings expected, byte[] expectedSecret, int expectedCount = 1)
    {
        using var connection = Open(path, SqliteOpenMode.ReadOnly);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id,region,secret,source,target,folder FROM settings WHERE id=1";
        using (var reader = command.ExecuteReader())
        {
            Assert.True(reader.Read());
            Assert.Equal(1, reader.GetInt32(0));
            Assert.Equal(expected.Region, reader.GetString(1));
            Assert.Equal(expectedSecret, (byte[])reader[2]);
            Assert.Equal(expected.Source, reader.GetString(3));
            Assert.Equal(expected.Target, reader.GetString(4));
            if (expected.Folder is null)
                Assert.True(reader.IsDBNull(5));
            else
                Assert.Equal(expected.Folder, reader.GetString(5));
            Assert.False(reader.Read());
        }
        command.CommandText = "SELECT COUNT(*) FROM settings";
        Assert.Equal((long)expectedCount, command.ExecuteScalar());
    }

    internal static Dictionary<string, byte[]> WriteOriginals(string path, int companionMask)
    {
        var originals = new Dictionary<string, byte[]>();
        for (var index = 0; index < Suffixes.Length; index++)
        {
            if (index > 0 && (companionMask & (1 << (index - 1))) == 0)
                continue;
            var bytes = System.Text.Encoding.UTF8.GetBytes($"corrupt original {index} 秘密");
            File.WriteAllBytes(path + Suffixes[index], bytes);
            originals.Add(Suffixes[index], bytes);
        }
        return originals;
    }

    internal static void AssertBackup(string path, string backup, Dictionary<string, byte[]> originals)
    {
        Assert.StartsWith(path + ".recovery-", backup);
        Assert.True(Guid.TryParseExact(backup[(path.Length + ".recovery-".Length)..], "N", out _));
        foreach (var suffix in Suffixes)
        {
            if (originals.TryGetValue(suffix, out var bytes))
                Assert.Equal(bytes, File.ReadAllBytes(backup + suffix));
            else
                Assert.False(File.Exists(backup + suffix));
        }
    }
}
