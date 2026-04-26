using Microsoft.Data.Sqlite;
using SpeechTranslatorDesktop.Services;

namespace SpeechTranslator.Desktop.Tests;

public class SqliteAzureAiServiceSettingsStoreTests : IDisposable
{
    private readonly string _testDirectory;

    public SqliteAzureAiServiceSettingsStoreTests()
    {
        _testDirectory = Path.Combine(AppContext.BaseDirectory, "test-artifacts", nameof(SqliteAzureAiServiceSettingsStoreTests), Guid.NewGuid().ToString("N"));
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsSettings()
    {
        var databasePath = Path.Combine(_testDirectory, "speech-translator-desktop.db");
        var protector = new FakeSecretProtector();
        var store = new SqliteAzureAiServiceSettingsStore(databasePath, protector);

        await store.SaveAsync(new AzureAiServiceSettings("japaneast", "plain-text-key"));
        var settings = await store.LoadAsync();

        settings.Should().NotBeNull();
        settings!.Region.Should().Be("japaneast");
        settings.ApiKey.Should().Be("plain-text-key");
    }

    [Fact]
    public async Task LoadAsync_WhenDirectoryAndDatabaseDoNotExist_ReturnsNull()
    {
        var databasePath = Path.Combine(_testDirectory, "nested", "speech-translator-desktop.db");
        var protector = new FakeSecretProtector();
        var store = new SqliteAzureAiServiceSettingsStore(databasePath, protector);

        var settings = await store.LoadAsync();

        settings.Should().BeNull();
        Directory.Exists(Path.GetDirectoryName(databasePath)!).Should().BeFalse();
        File.Exists(databasePath).Should().BeFalse();
    }

    [Fact]
    public async Task SaveAsync_StoresProtectedApiKeyInsteadOfPlainText()
    {
        var databasePath = Path.Combine(_testDirectory, "speech-translator-desktop.db");
        var protector = new FakeSecretProtector();
        var store = new SqliteAzureAiServiceSettingsStore(databasePath, protector);

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

        storedValue.Should().NotBeEmpty();
        storedValue.Should().NotEqual(Encoding.UTF8.GetBytes("plain-text-key"));
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
