using System;
using System.Data.Common;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PaletixDesktop.Settings;

namespace PaletixDesktop.Services
{
    public sealed class LocalDatabase
    {
        private static bool _sqliteRuntimeInitialized;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public string DatabasePath { get; }

        public LocalDatabase()
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AppConstants.AppName);

            DatabasePath = Path.Combine(folder, AppConstants.LocalDatabaseName);
        }

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);

            using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await ExecuteAsync(connection, """
                CREATE TABLE IF NOT EXISTS local_kv (
                    key TEXT PRIMARY KEY,
                    value TEXT NOT NULL,
                    updated_utc TEXT NOT NULL
                );
                """, cancellationToken);

            await ExecuteAsync(connection, """
                CREATE TABLE IF NOT EXISTS sync_outbox (
                    id TEXT PRIMARY KEY,
                    entity_name TEXT NOT NULL,
                    entity_id TEXT,
                    method TEXT NOT NULL,
                    endpoint TEXT NOT NULL,
                    payload TEXT NOT NULL,
                    status TEXT NOT NULL,
                    attempt_count INTEGER NOT NULL DEFAULT 0,
                    created_utc TEXT NOT NULL,
                    updated_utc TEXT NOT NULL
                );
                """, cancellationToken);

            await ExecuteAsync(connection, """
                CREATE TABLE IF NOT EXISTS sync_state (
                    entity_name TEXT PRIMARY KEY,
                    last_sync_utc TEXT
                );
                """, cancellationToken);
        }

        public async Task SetJsonAsync<T>(
            string key,
            T value,
            CancellationToken cancellationToken = default)
        {
            using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO local_kv (key, value, updated_utc)
                VALUES ($key, $value, $updatedUtc)
                ON CONFLICT(key) DO UPDATE SET
                    value = excluded.value,
                    updated_utc = excluded.updated_utc;
                """;
            command.AddParameter("$key", key);
            command.AddParameter("$value", JsonSerializer.Serialize(value, JsonOptions));
            command.AddParameter("$updatedUtc", DateTimeOffset.UtcNow.ToString("O"));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<T?> GetJsonAsync<T>(
            string key,
            CancellationToken cancellationToken = default)
        {
            using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT value FROM local_kv WHERE key = $key LIMIT 1;";
            command.AddParameter("$key", key);

            var value = await command.ExecuteScalarAsync(cancellationToken);
            return value is string json
                ? JsonSerializer.Deserialize<T>(json, JsonOptions)
                : default;
        }

        public async Task RemoveAsync(
            string key,
            CancellationToken cancellationToken = default)
        {
            using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM local_kv WHERE key = $key;";
            command.AddParameter("$key", key);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        internal DbConnection CreateConnection()
        {
            EnsureSqliteRuntimeInitialized();

            var connectionType = Type.GetType("Microsoft.Data.Sqlite.SqliteConnection, Microsoft.Data.Sqlite");
            if (connectionType is null)
            {
                throw new InvalidOperationException("Microsoft.Data.Sqlite no esta disponible en temps d'execucio.");
            }

            var connectionString = $"Data Source={DatabasePath};Cache=Shared";
            return (DbConnection)Activator.CreateInstance(connectionType, connectionString)!;
        }

        private static void EnsureSqliteRuntimeInitialized()
        {
            if (_sqliteRuntimeInitialized)
            {
                return;
            }

            var batteriesType = Type.GetType("SQLitePCL.Batteries_V2, SQLitePCLRaw.batteries_v2");
            batteriesType?.GetMethod("Init")?.Invoke(null, null);
            _sqliteRuntimeInitialized = true;
        }

        private static async Task ExecuteAsync(
            DbConnection connection,
            string sql,
            CancellationToken cancellationToken)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
