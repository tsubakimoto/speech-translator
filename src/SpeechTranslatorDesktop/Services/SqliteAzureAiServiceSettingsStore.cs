using Microsoft.Data.Sqlite;

namespace SpeechTranslatorDesktop.Services;

public sealed class SqliteAzureAiServiceSettingsStore : IAzureAiServiceSettingsStore
{
    private const string TableName = "azure_ai_service_settings";
    private readonly string _databasePath;
    private readonly ISecretProtector _secretProtector;

    public SqliteAzureAiServiceSettingsStore(string databasePath, ISecretProtector secretProtector)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(databasePath));
        }

        _databasePath = databasePath;
        _secretProtector = secretProtector ?? throw new ArgumentNullException(nameof(secretProtector));
    }

    public async Task<AzureAiServiceSettings?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_databasePath))
        {
            return null;
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureSchemaAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT region, protected_api_key
            FROM {TableName}
            WHERE id = 1;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var region = reader.GetString(0);
        var protectedApiKey = (byte[])reader["protected_api_key"];
        var apiKey = _secretProtector.Unprotect(protectedApiKey);

        return new AzureAiServiceSettings(region, apiKey);
    }

    public async Task SaveAsync(AzureAiServiceSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var directoryPath = Path.GetDirectoryName(_databasePath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureSchemaAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            INSERT INTO {TableName} (id, region, protected_api_key)
            VALUES (1, $region, $protectedApiKey)
            ON CONFLICT(id) DO UPDATE SET
                region = excluded.region,
                protected_api_key = excluded.protected_api_key;
            """;
        command.Parameters.AddWithValue("$region", settings.Region);
        command.Parameters.Add("$protectedApiKey", SqliteType.Blob).Value = _secretProtector.Protect(settings.ApiKey);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connectionStringBuilder = new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Pooling = false
        };

        var connection = new SqliteConnection(connectionStringBuilder.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static async Task EnsureSchemaAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            CREATE TABLE IF NOT EXISTS {TableName} (
                id INTEGER PRIMARY KEY CHECK (id = 1),
                region TEXT NOT NULL,
                protected_api_key BLOB NOT NULL
            );
            """;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
