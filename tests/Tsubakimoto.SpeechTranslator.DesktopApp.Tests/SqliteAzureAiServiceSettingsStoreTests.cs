using Microsoft.Data.Sqlite;

using Tsubakimoto.SpeechTranslator.DesktopApp.Services;

namespace Tsubakimoto.SpeechTranslator.DesktopApp.Tests;

public class SqliteAzureAiServiceSettingsStoreTests : IDisposable
{
    private readonly string _testDirectory;

    public SqliteAzureAiServiceSettingsStoreTests()
    {
        // Keep the SQLite journal path below the Windows MAX_PATH limit, even in a deep checkout.
        _testDirectory = Path.Combine(Path.GetTempPath(), "SpeechTranslatorDesktop.Tests", Guid.NewGuid().ToString("N"));
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsSettingsAsync()
    {
        // Arrange
        var databasePath = Path.Combine(_testDirectory, "speech-translator-desktop.db");
        var protector = new FakeSecretProtector();
        var store = new SqliteAzureAiServiceSettingsStore(databasePath, protector);

        // Act
        await store.SaveAsync(new AzureAiServiceSettings("japaneast", "plain-text-key"));
        var settings = await store.LoadAsync();

        // Assert
        Assert.NotNull(settings);
        Assert.Equal("japaneast", settings!.Region);
        Assert.Equal("plain-text-key", settings.ApiKey);
    }

    [Fact]
    public async Task LoadAsync_WhenDirectoryAndDatabaseDoNotExist_ReturnsNullAsync()
    {
        // Arrange
        var databasePath = Path.Combine(_testDirectory, "nested", "speech-translator-desktop.db");
        var protector = new FakeSecretProtector();
        var store = new SqliteAzureAiServiceSettingsStore(databasePath, protector);

        // Act
        var settings = await store.LoadAsync();

        // Assert
        Assert.Null(settings);
        Assert.False(Directory.Exists(Path.GetDirectoryName(databasePath)!));
        Assert.False(File.Exists(databasePath));
    }

    [Fact]
    public async Task SaveAsync_StoresProtectedApiKeyInsteadOfPlainTextAsync()
    {
        // Arrange
        var databasePath = Path.Combine(_testDirectory, "speech-translator-desktop.db");
        var protector = new FakeSecretProtector();
        var store = new SqliteAzureAiServiceSettingsStore(databasePath, protector);

        // Act
        await store.SaveAsync(new AzureAiServiceSettings("japaneast", "plain-text-key"));

        var connectionStringBuilder = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Pooling = false
        };

        await using var connection = new SqliteConnection(connectionStringBuilder.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT protected_api_key FROM azure_ai_service_settings WHERE id = 1;";
        var storedValue = (byte[])(await command.ExecuteScalarAsync())!;

        // Assert
        Assert.NotEmpty(storedValue);
        Assert.NotEqual<byte>(Encoding.UTF8.GetBytes("plain-text-key"), storedValue);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }

    private sealed class FakeSecretProtector : ISecretProtector
    {
        public byte[] Protect(string plaintext)
        {
            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            Array.Reverse(plaintextBytes);
            return plaintextBytes;
        }

        public string Unprotect(byte[] protectedData)
        {
            var plaintextBytes = protectedData.ToArray();
            Array.Reverse(plaintextBytes);
            return Encoding.UTF8.GetString(plaintextBytes);
        }
    }
}