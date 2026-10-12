using Microsoft.Data.Sqlite;
using NSubstitute.ExceptionExtensions;
using NSubstitute.Extensions;
using System.Security.Cryptography;
using System.Text;
using Tsubakimoto.SpeechTranslator.WinUIApp.Core;

namespace Tsubakimoto.SpeechTranslator.WinUIApp.Tests;

/// <summary>Tests isolated SQLite durability, sticky read locks and explicit preservation recovery.</summary>
public sealed class SettingsStoreTests
{
    private static Settings Original => new("japaneast", "original-key", "ja-JP", "en-US", "旧 folder");
    private static Settings Replacement => new("westus", "replacement-key", "en-US", "ja-JP", null);

    private static ISecretProtector CreateProtector()
    {
        var protector = Substitute.For<ISecretProtector>();
        protector.Protect(Arg.Any<string>()).Returns(SqliteTestDatabase.Payload);
        protector.Unprotect(Arg.Any<byte[]>()).Returns(Original.Key);
        return protector;
    }

    private static void AssertSettings(Settings expected, Settings? actual)
    {
        Assert.NotNull(actual);
        Assert.Equal(expected.Region, actual.Region);
        Assert.Equal(expected.Key, actual.Key);
        Assert.Equal(expected.Source, actual.Source);
        Assert.Equal(expected.Target, actual.Target);
        Assert.Equal(expected.Folder, actual.Folder);
    }

    private static async Task AssertWritesLockedAsync(SettingsStore store, ISecretProtector protector)
    {
        protector.ClearReceivedCalls();
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveAsync(Replacement));
        Assert.Equal("Settings must be loaded successfully or explicitly recovered before saving.", error.Message);
        protector.DidNotReceive().Protect(Arg.Any<string>());
    }

    /// <summary>Distinguishes a missing nested database from failed reads without creating directories.</summary>
    [Fact]
    public async Task LoadAsync_MissingDatabase_ReturnsNullWithoutCreatingAnythingAsync()
    {
        // Arrange
        using var directory = new TestDirectory();
        var path = directory.GetPath("nested/deeper/settings.db");
        var protector = CreateProtector();
        var store = new SettingsStore(path, protector);
        var before = directory.Snapshot();

        // Act
        var settings = await store.LoadAsync();

        // Assert
        Assert.Null(settings);
        directory.AssertUnchanged(before);
        Assert.Empty(protector.ReceivedCalls());
    }

    /// <summary>Requires a successful load before even encrypting or creating a database.</summary>
    [Fact]
    public async Task SaveAsync_BeforeSuccessfulLoad_RejectsWithoutWritesAsync()
    {
        // Arrange
        using var directory = new TestDirectory();
        var protector = CreateProtector();
        var store = new SettingsStore(directory.GetPath("nested/settings.db"), protector);
        var before = directory.Snapshot();

        // Act & Assert
        await AssertWritesLockedAsync(store, protector);
        directory.AssertUnchanged(before);
        Assert.Empty(protector.ReceivedCalls());
    }

    /// <summary>Reads no row or an unrelated id without changing bytes, then permits a save.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LoadAsync_ExistingEmptySettingsTable_ReturnsNullWithoutMutatingDatabaseAsync(bool unrelatedRow)
    {
        // Arrange
        using var directory = new TestDirectory();
        var path = directory.GetPath("settings.db");
        SqliteTestDatabase.Create(path, unrelatedRow ? Original : null, id: 2);
        var protector = CreateProtector();
        var store = new SettingsStore(path, protector);
        var before = directory.Snapshot();

        // Act
        var settings = await store.LoadAsync();

        // Assert
        Assert.Null(settings);
        directory.AssertUnchanged(before);
        Assert.Empty(protector.ReceivedCalls());
        await store.SaveAsync(Replacement);
        SqliteTestDatabase.AssertRow(path, Replacement, SqliteTestDatabase.Payload, unrelatedRow ? 2 : 1);
        protector.Received(1).Protect(Replacement.Key);
    }

    /// <summary>Restores every preference and credential from read-only attributed and normal files.</summary>
    [Theory]
    [InlineData("en-US", "ja-JP", null, false)]
    [InlineData("ja-JP", "en-US", "", false)]
    [InlineData("en-US", "en-US", "C:\\録音's", false)]
    [InlineData("ja-JP", "ja-JP", null, false)]
    [InlineData("en-US", "ja-JP", "", true)]
    [InlineData("ja-JP", "en-US", "C:\\録音's", true)]
    public async Task LoadAsync_ExistingRow_RestoresEveryFieldAsync(string source, string target, string? folder, bool readOnly)
    {
        // Arrange
        using var directory = new TestDirectory();
        var path = directory.GetPath("settings.db");
        var expected = Original with { Source = source, Target = target, Folder = folder };
        SqliteTestDatabase.Create(path, expected);
        var protector = CreateProtector();
        var store = new SettingsStore(path, protector);
        var attributes = File.GetAttributes(path);
        if (readOnly)
            File.SetAttributes(path, attributes | FileAttributes.ReadOnly);
        var before = directory.Snapshot();
        try
        {
            // Act
            var actual = await store.LoadAsync();

            // Assert
            AssertSettings(expected, actual);
            directory.AssertUnchanged(before);
            Assert.Equal(readOnly, File.GetAttributes(path).HasFlag(FileAttributes.ReadOnly));
            protector.Received(1).Unprotect(Arg.Is<byte[]>(bytes => bytes.SequenceEqual(SqliteTestDatabase.Payload)));
            protector.DidNotReceive().Protect(Arg.Any<string>());
        }
        finally
        {
            File.SetAttributes(path, attributes);
        }
    }

    /// <summary>Creates only on save, encrypts parameters and replaces all columns with one durable row.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("C:\\録音's")]
    public async Task SaveAsync_AfterEmptyLoad_RoundTripsAndUpsertsOneRowAsync(string? folder)
    {
        // Arrange
        using var directory = new TestDirectory();
        var path = directory.GetPath("nested/deeper/settings.db");
        var protector = CreateProtector();
        byte[] firstCipher = [0xF1, 0x21, 0x67];
        byte[] nextCipher = [0xA8, 0x00, 0xE3];
        var first = new Settings("region'日本", "key'秘密", "ja-JP", "ja-JP", folder);
        protector.Protect(first.Key).Returns(firstCipher);
        protector.Protect(Replacement.Key).Returns(nextCipher);
        protector.Unprotect(Arg.Is<byte[]>(b => b.SequenceEqual(nextCipher))).Returns(Replacement.Key);
        var store = new SettingsStore(path, protector);
        Assert.Null(await store.LoadAsync());
        Assert.False(Directory.Exists(Path.GetDirectoryName(path)));

        // Act
        await store.SaveAsync(first);

        // Assert
        Assert.True(Directory.Exists(Path.GetDirectoryName(path)));
        SqliteTestDatabase.AssertRow(path, first, firstCipher);
        Assert.False(firstCipher.SequenceEqual(Encoding.UTF8.GetBytes(first.Key)));
        protector.Received(1).Protect(first.Key);
        await store.SaveAsync(Replacement);
        SqliteTestDatabase.AssertRow(path, Replacement, nextCipher);
        AssertSettings(Replacement, await new SettingsStore(path, protector).LoadAsync());
        protector.Received(1).Protect(Replacement.Key);
        protector.Received(1).Unprotect(Arg.Is<byte[]>(b => b.SequenceEqual(nextCipher)));
    }

    /// <summary>Persists supplied credential and language strings without inventing save-time validation.</summary>
    [Theory]
    [InlineData("fr-FR", "ja-JP")]
    [InlineData("en-US", "")]
    public async Task SaveAsync_UnsupportedLanguages_PersistsThenLoadLocksWritesAsync(string source, string target)
    {
        // Arrange
        using var directory = new TestDirectory();
        var path = directory.GetPath("settings.db");
        var protector = CreateProtector();
        var store = new SettingsStore(path, protector);
        Assert.Null(await store.LoadAsync());
        var invalid = new Settings("INVALID region", "key with spaces", source, target, "");

        // Act
        await store.SaveAsync(invalid);

        // Assert
        SqliteTestDatabase.AssertRow(path, invalid, SqliteTestDatabase.Payload);
        protector.Received(1).Protect(invalid.Key);
        var before = directory.Snapshot();
        await Assert.ThrowsAsync<InvalidDataException>(() => store.LoadAsync());
        await AssertWritesLockedAsync(store, protector);
        directory.AssertUnchanged(before);
    }

    private static void ArrangeReadFailure(string path, ISecretProtector protector, string kind)
    {
        if (kind == "corrupt")
        {
            File.WriteAllBytes(path, Encoding.UTF8.GetBytes("not a SQLite database 日本"));
            return;
        }
        if (kind == "missing-table")
        {
            SqliteTestDatabase.Execute(path, "CREATE TABLE unrelated(value TEXT)");
            return;
        }
        SqliteTestDatabase.Create(path, Original);
        switch (kind)
        {
            case "missing-column": SqliteTestDatabase.Execute(path, "ALTER TABLE settings RENAME COLUMN folder TO other"); break;
            case "text-secret": SqliteTestDatabase.Execute(path, "UPDATE settings SET secret='not a blob'"); break;
            case "null-secret": SqliteTestDatabase.Execute(path, "UPDATE settings SET secret=NULL"); break;
            case "null-region": SqliteTestDatabase.Execute(path, "UPDATE settings SET region=NULL"); break;
            case "null-source": SqliteTestDatabase.Execute(path, "UPDATE settings SET source=NULL"); break;
            case "null-target": SqliteTestDatabase.Execute(path, "UPDATE settings SET target=NULL"); break;
            case "decrypt": protector.Unprotect(Arg.Any<byte[]>()).Throws(new CryptographicException("cannot decrypt")); break;
            case "source": SqliteTestDatabase.Execute(path, "UPDATE settings SET source='fr-FR'"); break;
            case "target": SqliteTestDatabase.Execute(path, "UPDATE settings SET target='EN-us'"); break;
            default: throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    /// <summary>Rejects corrupt/schema/type/null/cipher/language failures without implicit recovery or writes.</summary>
    [Theory]
    [InlineData("corrupt", typeof(SqliteException))]
    [InlineData("missing-table", typeof(SqliteException))]
    [InlineData("missing-column", typeof(SqliteException))]
    [InlineData("text-secret", typeof(InvalidCastException))]
    [InlineData("null-secret", typeof(InvalidCastException))]
    [InlineData("null-region", typeof(InvalidOperationException))]
    [InlineData("null-source", typeof(InvalidOperationException))]
    [InlineData("null-target", typeof(InvalidOperationException))]
    [InlineData("decrypt", typeof(CryptographicException))]
    [InlineData("source", typeof(InvalidDataException))]
    [InlineData("target", typeof(InvalidDataException))]
    public async Task LoadAsync_CorruptSchemaOrCipher_LocksWritesUntilExplicitRecoveryAsync(string kind, Type errorType)
    {
        // Arrange
        using var directory = new TestDirectory();
        var path = directory.GetPath("settings.db");
        var protector = CreateProtector();
        ArrangeReadFailure(path, protector, kind);
        var store = new SettingsStore(path, protector);
        var before = directory.Snapshot();

        // Act
        var error = await Record.ExceptionAsync(() => store.LoadAsync());

        // Assert
        Assert.NotNull(error);
        Assert.Equal(errorType, error.GetType());
        protector.DidNotReceive().Protect(Arg.Any<string>());
        if (kind is "decrypt" or "source" or "target" or "null-source" or "null-target")
            protector.Received(1).Unprotect(Arg.Is<byte[]>(b => b.SequenceEqual(SqliteTestDatabase.Payload)));
        else
            protector.DidNotReceive().Unprotect(Arg.Any<byte[]>());
        directory.AssertUnchanged(before);
        await AssertWritesLockedAsync(store, protector);
        directory.AssertUnchanged(before);
        Assert.Empty(Directory.GetFiles(directory.Root, "*.recovery-*"));
    }

    /// <summary>Keeps prior read errors sticky after externally repaired or removed files until recovery succeeds.</summary>
    [Theory]
    [InlineData("corrupt", false)]
    [InlineData("corrupt", true)]
    [InlineData("decrypt", false)]
    [InlineData("decrypt", true)]
    [InlineData("source", false)]
    [InlineData("source", true)]
    [InlineData("target", false)]
    [InlineData("target", true)]
    public async Task LoadAsync_LaterSuccessOrMissingFile_DoesNotClearReadErrorAsync(string kind, bool remove)
    {
        // Arrange
        using var directory = new TestDirectory();
        var path = directory.GetPath("settings.db");
        var protector = CreateProtector();
        ArrangeReadFailure(path, protector, kind);
        var store = new SettingsStore(path, protector);
        Assert.NotNull(await Record.ExceptionAsync(() => store.LoadAsync()));
        File.Delete(path);
        protector.Unprotect(Arg.Any<byte[]>()).Returns(Original.Key);
        if (!remove)
            SqliteTestDatabase.Create(path, Original);
        var before = directory.Snapshot();

        // Act
        var later = await store.LoadAsync();

        // Assert
        if (remove)
            Assert.Null(later);
        else
            AssertSettings(Original, later);
        await AssertWritesLockedAsync(store, protector);
        directory.AssertUnchanged(before);
        await store.RecoverAsync(Replacement);
        await store.SaveAsync(Original);
        SqliteTestDatabase.AssertRow(path, Original, SqliteTestDatabase.Payload);
    }

    /// <summary>Protection failures preserve existing rows or missing parents and permit an ordinary retry.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveAsync_ProtectionFailure_PreservesPriorSettingsAndPermitsRetryAsync(bool existing)
    {
        // Arrange
        using var directory = new TestDirectory();
        var path = directory.GetPath("nested/settings.db");
        if (existing)
            SqliteTestDatabase.Create(path, Original);
        var protector = CreateProtector();
        var store = new SettingsStore(path, protector);
        await store.LoadAsync();
        var failure = new CryptographicException("protect failed");
        protector.Protect(Replacement.Key).Throws(failure);
        var before = directory.Snapshot();

        // Act
        var error = await Assert.ThrowsAsync<CryptographicException>(() => store.SaveAsync(Replacement));

        // Assert
        Assert.Same(failure, error);
        directory.AssertUnchanged(before);
        if (existing)
            SqliteTestDatabase.AssertRow(path, Original, SqliteTestDatabase.Payload);
        else
            Assert.False(Directory.Exists(Path.GetDirectoryName(path)));
        protector.Received(1).Protect(Replacement.Key);
        protector.Configure().Protect(Replacement.Key).Returns(SqliteTestDatabase.Payload);
        await store.SaveAsync(Replacement);
        SqliteTestDatabase.AssertRow(path, Replacement, SqliteTestDatabase.Payload);
        protector.Received(2).Protect(Replacement.Key);
    }

    /// <summary>Exclusive sharing blocks database access immediately without partial updates or sticky save errors.</summary>
    [Fact]
    public async Task SaveAsync_ExclusiveFileLock_PreservesPriorRowAndPermitsRetryAsync()
    {
        // Arrange
        using var directory = new TestDirectory();
        var path = directory.GetPath("settings.db");
        SqliteTestDatabase.Create(path, Original);
        var protector = CreateProtector();
        var store = new SettingsStore(path, protector);
        await store.LoadAsync();
        var before = directory.Snapshot();

        // Act & Assert
        using (var handle = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            var error = await Assert.ThrowsAsync<SqliteException>(() => store.SaveAsync(Replacement));
            Assert.Equal(14, error.SqliteErrorCode);
        }
        directory.AssertUnchanged(before);
        SqliteTestDatabase.AssertRow(path, Original, SqliteTestDatabase.Payload);
        protector.Received(1).Protect(Replacement.Key);
        await store.SaveAsync(Replacement);
        SqliteTestDatabase.AssertRow(path, Replacement, SqliteTestDatabase.Payload);
        protector.Received(2).Protect(Replacement.Key);
    }

    /// <summary>A constraint failure rolls back SQL changes and leaves normal retry available.</summary>
    [Fact]
    public async Task SaveAsync_SqlConstraintFailure_RollsBackAndPermitsRetryAsync()
    {
        // Arrange
        using var directory = new TestDirectory();
        var path = directory.GetPath("settings.db");
        SqliteTestDatabase.Create(path, Original);
        SqliteTestDatabase.Execute(path, "CREATE TRIGGER reject_update BEFORE UPDATE ON settings BEGIN SELECT RAISE(ABORT,'reject replacement'); END");
        var protector = CreateProtector();
        var store = new SettingsStore(path, protector);
        await store.LoadAsync();
        var before = directory.Snapshot();

        // Act
        var error = await Assert.ThrowsAsync<SqliteException>(() => store.SaveAsync(Replacement));

        // Assert
        Assert.Equal(19, error.SqliteErrorCode);
        directory.AssertUnchanged(before);
        SqliteTestDatabase.AssertRow(path, Original, SqliteTestDatabase.Payload);
        SqliteTestDatabase.Execute(path, "DROP TRIGGER reject_update");
        await store.SaveAsync(Replacement);
        SqliteTestDatabase.AssertRow(path, Replacement, SqliteTestDatabase.Payload);
        protector.Received(2).Protect(Replacement.Key);
    }

    /// <summary>An exclusive read-access failure leaves originals untouched and saves locked after release.</summary>
    [Fact]
    public async Task LoadAsync_AccessFailure_DoesNotCreateOrRecoverAndLocksWritesAsync()
    {
        // Arrange
        using var directory = new TestDirectory();
        var path = directory.GetPath("settings.db");
        SqliteTestDatabase.Create(path, Original);
        var protector = CreateProtector();
        var store = new SettingsStore(path, protector);
        var before = directory.Snapshot();

        // Act & Assert
        using (var handle = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            var error = await Assert.ThrowsAsync<SqliteException>(() => store.LoadAsync());
            Assert.Equal(14, error.SqliteErrorCode);
        }
        Assert.Empty(protector.ReceivedCalls());
        directory.AssertUnchanged(before);
        await AssertWritesLockedAsync(store, protector);
        directory.AssertUnchanged(before);
        Assert.Empty(Directory.GetFiles(directory.Root, "*.recovery-*"));
    }

    /// <summary>Explicit recovery retains exact corrupt base and every present companion combination.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public async Task RecoverAsync_CorruptDatabaseAndCompanions_PreservesOriginalBytesAsync(int companionMask)
    {
        // Arrange
        using var directory = new TestDirectory();
        var path = directory.GetPath("settings.db");
        var originals = SqliteTestDatabase.WriteOriginals(path, companionMask);
        var protector = CreateProtector();
        protector.Unprotect(Arg.Any<byte[]>()).Returns(Replacement.Key);
        var store = new SettingsStore(path, protector);

        // Act
        var backup = await store.RecoverAsync(Replacement);

        // Assert
        SqliteTestDatabase.AssertBackup(path, backup, originals);
        SqliteTestDatabase.AssertRow(path, Replacement, SqliteTestDatabase.Payload);
        foreach (var suffix in SqliteTestDatabase.Suffixes.Skip(1))
            Assert.False(File.Exists(path + suffix));
        Assert.Equal(originals.Count + 1, Directory.GetFiles(directory.Root).Length);
        AssertSettings(Replacement, await store.LoadAsync());
        protector.Received(1).Protect(Replacement.Key);
        protector.Received(1).Unprotect(Arg.Is<byte[]>(b => b.SequenceEqual(SqliteTestDatabase.Payload)));
        await store.SaveAsync(Original);
        SqliteTestDatabase.AssertRow(path, Original, SqliteTestDatabase.Payload);
        SqliteTestDatabase.AssertBackup(path, backup, originals);
    }

    /// <summary>Failed replacement keeps backups, does not restore automatically and locks even a loaded store.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RecoverAsync_FailedReplacement_PreservesBackupAndLocksWritesAsync(bool previouslyLoaded)
    {
        // Arrange
        using var directory = new TestDirectory();
        var path = directory.GetPath("settings.db");
        var protector = CreateProtector();
        var store = new SettingsStore(path, protector);
        if (previouslyLoaded)
        {
            SqliteTestDatabase.Create(path, Original);
            await store.LoadAsync();
        }
        else
        {
            File.WriteAllBytes(path, [0xFF, 0xAC]);
            await Assert.ThrowsAsync<SqliteException>(() => store.LoadAsync());
        }
        var originals = new Dictionary<string, byte[]> { [""] = File.ReadAllBytes(path) };
        foreach (var suffix in SqliteTestDatabase.Suffixes.Skip(1))
        {
            var bytes = Encoding.UTF8.GetBytes("inert " + suffix);
            File.WriteAllBytes(path + suffix, bytes);
            originals.Add(suffix, bytes);
        }
        var failure = new CryptographicException("replacement protection failed");
        protector.Protect(Replacement.Key).Throws(failure);

        // Act
        var error = await Assert.ThrowsAsync<CryptographicException>(() => store.RecoverAsync(Replacement));

        // Assert
        Assert.Same(failure, error);
        var backup = Assert.Single(Directory.GetFiles(directory.Root, "settings.db.recovery-*"), p => !SqliteTestDatabase.Suffixes.Skip(1).Any(p.EndsWith));
        SqliteTestDatabase.AssertBackup(path, backup, originals);
        foreach (var suffix in SqliteTestDatabase.Suffixes)
            Assert.False(File.Exists(path + suffix));
        protector.Received(1).Protect(Replacement.Key);
        var before = directory.Snapshot();
        await AssertWritesLockedAsync(store, protector);
        directory.AssertUnchanged(before);
        protector.Configure().Protect(Replacement.Key).Returns(SqliteTestDatabase.Payload);
        var second = await store.RecoverAsync(Replacement);
        Assert.NotEqual(backup, second);
        Assert.False(File.Exists(second));
        SqliteTestDatabase.AssertRow(path, Replacement, SqliteTestDatabase.Payload);
        await store.SaveAsync(Original);
        SqliteTestDatabase.AssertBackup(path, backup, originals);
    }

    /// <summary>Recovery without an original creates only settings and uses unique names on subsequent recovery.</summary>
    [Fact]
    public async Task RecoverAsync_MissingOriginal_CreatesSettingsWithoutFabricatingBackupFileAsync()
    {
        // Arrange
        using var directory = new TestDirectory();
        var path = directory.GetPath("nested/settings.db");
        var protector = CreateProtector();
        var store = new SettingsStore(path, protector);

        // Act
        var first = await store.RecoverAsync(Replacement);

        // Assert
        SqliteTestDatabase.AssertBackup(path, first, []);
        Assert.False(File.Exists(first));
        SqliteTestDatabase.AssertRow(path, Replacement, SqliteTestDatabase.Payload);
        var originalBytes = File.ReadAllBytes(path);
        var second = await store.RecoverAsync(Original);
        Assert.NotEqual(first, second);
        SqliteTestDatabase.AssertBackup(path, second, new() { [""] = originalBytes });
        SqliteTestDatabase.AssertRow(path, Original, SqliteTestDatabase.Payload);
        AssertSettings(Original, await store.LoadAsync());
        await store.SaveAsync(Replacement);
        Assert.Equal(originalBytes, File.ReadAllBytes(second));
    }

    /// <summary>Orphan companions are retained even when the base database is absent.</summary>
    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    public async Task RecoverAsync_MissingBaseWithOrphanCompanions_PreservesEveryCompanionAsync(int companionMask)
    {
        // Arrange
        using var directory = new TestDirectory();
        var path = directory.GetPath("settings.db");
        var originals = SqliteTestDatabase.WriteOriginals(path, companionMask);
        File.Delete(path);
        originals.Remove("");
        var protector = CreateProtector();
        var store = new SettingsStore(path, protector);
        var before = directory.Snapshot();
        Assert.Null(await store.LoadAsync());
        directory.AssertUnchanged(before);

        // Act
        var backup = await store.RecoverAsync(Replacement);

        // Assert
        SqliteTestDatabase.AssertBackup(path, backup, originals);
        Assert.False(File.Exists(backup));
        SqliteTestDatabase.AssertRow(path, Replacement, SqliteTestDatabase.Payload);
        Assert.Equal(originals.Count + 1, Directory.GetFiles(directory.Root).Length);
        foreach (var suffix in SqliteTestDatabase.Suffixes.Skip(1))
            Assert.False(File.Exists(path + suffix));
        protector.Received(1).Protect(Replacement.Key);
        await store.SaveAsync(Original);
        SqliteTestDatabase.AssertBackup(path, backup, originals);
    }

    /// <summary>A read-only database save failure preserves bytes and permits retry after attribute restoration.</summary>
    [Fact]
    public async Task SaveAsync_ReadOnlyFile_PreservesPriorRowAndPermitsRetryAsync()
    {
        // Arrange
        using var directory = new TestDirectory();
        var path = directory.GetPath("settings.db");
        SqliteTestDatabase.Create(path, Original);
        var protector = CreateProtector();
        var store = new SettingsStore(path, protector);
        await store.LoadAsync();
        var attributes = File.GetAttributes(path);
        File.SetAttributes(path, attributes | FileAttributes.ReadOnly);
        var before = directory.Snapshot();
        try
        {
            // Act
            var error = await Assert.ThrowsAsync<SqliteException>(() => store.SaveAsync(Replacement));

            // Assert
            Assert.Equal(8, error.SqliteErrorCode);
            directory.AssertUnchanged(before);
            SqliteTestDatabase.AssertRow(path, Original, SqliteTestDatabase.Payload);
        }
        finally
        {
            File.SetAttributes(path, attributes);
        }
        await store.SaveAsync(Replacement);
        SqliteTestDatabase.AssertRow(path, Replacement, SqliteTestDatabase.Payload);
        protector.Received(2).Protect(Replacement.Key);
    }

    /// <summary>Sharing-denied moves preserve data at original or moved locations without promising rollback.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("-wal")]
    [InlineData("-shm")]
    [InlineData("-journal")]
    public async Task RecoverAsync_MoveFailure_PreservesAllExistingDataAsync(string lockedSuffix)
    {
        // Arrange
        using var directory = new TestDirectory();
        var path = directory.GetPath("settings.db");
        File.WriteAllBytes(path, [0xFF, 0x91]);
        var protector = CreateProtector();
        var store = new SettingsStore(path, protector);
        await Assert.ThrowsAsync<SqliteException>(() => store.LoadAsync());
        var originals = SqliteTestDatabase.WriteOriginals(path, 7);
        protector.ClearReceivedCalls();

        // Act & Assert
        using (var handle = new FileStream(path + lockedSuffix, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            await Assert.ThrowsAsync<IOException>(() => store.RecoverAsync(Replacement));
        Assert.Empty(protector.ReceivedCalls());
        var moved = Directory.GetFiles(directory.Root, "settings.db.recovery-*");
        string? backup = lockedSuffix == "" ? null : Assert.Single(moved, p => !SqliteTestDatabase.Suffixes.Skip(1).Any(p.EndsWith));
        var failingIndex = Array.IndexOf(SqliteTestDatabase.Suffixes, lockedSuffix);
        for (var index = 0; index < SqliteTestDatabase.Suffixes.Length; index++)
        {
            var suffix = SqliteTestDatabase.Suffixes[index];
            var retained = index < failingIndex ? backup + suffix : path + suffix;
            Assert.Equal(originals[suffix], File.ReadAllBytes(retained));
            var other = index < failingIndex ? path + suffix : backup + suffix;
            if (backup is not null)
                Assert.False(File.Exists(other));
        }
        Assert.Equal(4, Directory.GetFiles(directory.Root).Length);
        var before = directory.Snapshot();
        await AssertWritesLockedAsync(store, protector);
        directory.AssertUnchanged(before);
    }
}
