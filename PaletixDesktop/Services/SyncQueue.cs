using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PaletixDesktop.Services
{
    public sealed record SyncQueueItem(
        string Id,
        string EntityName,
        string? EntityId,
        string Method,
        string Endpoint,
        string Payload,
        string Status,
        int AttemptCount,
        DateTimeOffset CreatedUtc,
        DateTimeOffset UpdatedUtc);

    public sealed class SyncQueue
    {
        private readonly LocalDatabase _database;

        public SyncQueue(LocalDatabase database)
        {
            _database = database;
        }

        public async Task EnqueueAsync(
            string entityName,
            string? entityId,
            string method,
            string endpoint,
            object payload,
            CancellationToken cancellationToken = default)
        {
            await UpsertAsync(entityName, entityId, method, endpoint, payload, "Pending", cancellationToken);
        }

        public async Task<string> UpsertAsync(
            string entityName,
            string? entityId,
            string method,
            string endpoint,
            object payload,
            string status = "Pending",
            CancellationToken cancellationToken = default)
        {
            using var connection = _database.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            var now = DateTimeOffset.UtcNow.ToString("O");

            using (var lookup = connection.CreateCommand())
            {
                lookup.CommandText = """
                    SELECT id FROM sync_outbox
                    WHERE entity_name = $entityName
                      AND IFNULL(entity_id, '') = IFNULL($entityId, '')
                      AND method = $method
                      AND status IN ('Pending', 'Error')
                    ORDER BY created_utc DESC
                    LIMIT 1;
                    """;
                lookup.AddParameter("$entityName", entityName);
                lookup.AddParameter("$entityId", entityId);
                lookup.AddParameter("$method", method);

                if (await lookup.ExecuteScalarAsync(cancellationToken) is string existingId)
                {
                    using var update = connection.CreateCommand();
                    update.CommandText = """
                        UPDATE sync_outbox
                        SET endpoint = $endpoint,
                            payload = $payload,
                            status = $status,
                            updated_utc = $updatedUtc
                        WHERE id = $id;
                        """;
                    update.AddParameter("$id", existingId);
                    update.AddParameter("$endpoint", endpoint);
                    update.AddParameter("$payload", JsonSerializer.Serialize(payload));
                    update.AddParameter("$status", status);
                    update.AddParameter("$updatedUtc", now);
                    await update.ExecuteNonQueryAsync(cancellationToken);
                    return existingId;
                }
            }

            var id = Guid.NewGuid().ToString("N");
            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO sync_outbox
                    (id, entity_name, entity_id, method, endpoint, payload, status, created_utc, updated_utc)
                VALUES
                    ($id, $entityName, $entityId, $method, $endpoint, $payload, $status, $createdUtc, $updatedUtc);
                """;
            command.AddParameter("$id", id);
            command.AddParameter("$entityName", entityName);
            command.AddParameter("$entityId", entityId);
            command.AddParameter("$method", method);
            command.AddParameter("$endpoint", endpoint);
            command.AddParameter("$payload", JsonSerializer.Serialize(payload));
            command.AddParameter("$status", status);
            command.AddParameter("$createdUtc", now);
            command.AddParameter("$updatedUtc", now);

            await command.ExecuteNonQueryAsync(cancellationToken);
            return id;
        }

        public async Task<IReadOnlyList<SyncQueueItem>> GetActiveAsync(
            string entityName,
            CancellationToken cancellationToken = default)
        {
            return await GetByStatusAsync(entityName, includeErrors: true, cancellationToken);
        }

        public async Task<IReadOnlyList<SyncQueueItem>> GetPendingAsync(
            string entityName,
            CancellationToken cancellationToken = default)
        {
            return await GetByStatusAsync(entityName, includeErrors: false, cancellationToken);
        }

        public async Task RemoveAsync(string id, CancellationToken cancellationToken = default)
        {
            using var connection = _database.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM sync_outbox WHERE id = $id;";
            command.AddParameter("$id", id);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task RemoveForEntityAsync(
            string entityName,
            string? entityId,
            CancellationToken cancellationToken = default)
        {
            using var connection = _database.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = """
                DELETE FROM sync_outbox
                WHERE entity_name = $entityName
                  AND IFNULL(entity_id, '') = IFNULL($entityId, '')
                  AND status IN ('Pending', 'Error');
                """;
            command.AddParameter("$entityName", entityName);
            command.AddParameter("$entityId", entityId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task MarkCompletedAsync(string id, CancellationToken cancellationToken = default)
        {
            await UpdateStatusAsync(id, "Completed", incrementAttempt: true, cancellationToken);
        }

        public async Task MarkFailedAsync(string id, CancellationToken cancellationToken = default)
        {
            await UpdateStatusAsync(id, "Error", incrementAttempt: true, cancellationToken);
        }

        public async Task MarkPendingAsync(string id, CancellationToken cancellationToken = default)
        {
            await UpdateStatusAsync(id, "Pending", incrementAttempt: false, cancellationToken);
        }

        public async Task MarkErrorsPendingAsync(
            string entityName,
            CancellationToken cancellationToken = default)
        {
            using var connection = _database.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = """
                UPDATE sync_outbox
                SET status = 'Pending',
                    updated_utc = $updatedUtc
                WHERE entity_name = $entityName
                  AND status = 'Error';
                """;
            command.AddParameter("$entityName", entityName);
            command.AddParameter("$updatedUtc", DateTimeOffset.UtcNow.ToString("O"));
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default)
        {
            using var connection = _database.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sync_outbox WHERE status IN ('Pending', 'Error');";
            var value = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(value);
        }

        private async Task<IReadOnlyList<SyncQueueItem>> GetByStatusAsync(
            string entityName,
            bool includeErrors,
            CancellationToken cancellationToken)
        {
            using var connection = _database.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = includeErrors
                ? """
                  SELECT id, entity_name, entity_id, method, endpoint, payload, status, attempt_count, created_utc, updated_utc
                  FROM sync_outbox
                  WHERE entity_name = $entityName AND status IN ('Pending', 'Error')
                  ORDER BY created_utc ASC;
                  """
                : """
                  SELECT id, entity_name, entity_id, method, endpoint, payload, status, attempt_count, created_utc, updated_utc
                  FROM sync_outbox
                  WHERE entity_name = $entityName AND status = 'Pending'
                  ORDER BY created_utc ASC;
                  """;
            command.AddParameter("$entityName", entityName);

            var items = new List<SyncQueueItem>();
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(new SyncQueueItem(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.IsDBNull(2) ? null : reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetString(5),
                    reader.GetString(6),
                    reader.GetInt32(7),
                    DateTimeOffset.Parse(reader.GetString(8)),
                    DateTimeOffset.Parse(reader.GetString(9))));
            }

            return items;
        }

        private async Task UpdateStatusAsync(
            string id,
            string status,
            bool incrementAttempt,
            CancellationToken cancellationToken)
        {
            using var connection = _database.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = incrementAttempt
                ? """
                  UPDATE sync_outbox
                  SET status = $status,
                      attempt_count = attempt_count + 1,
                      updated_utc = $updatedUtc
                  WHERE id = $id;
                  """
                : """
                  UPDATE sync_outbox
                  SET status = $status,
                      updated_utc = $updatedUtc
                  WHERE id = $id;
                  """;
            command.AddParameter("$id", id);
            command.AddParameter("$status", status);
            command.AddParameter("$updatedUtc", DateTimeOffset.UtcNow.ToString("O"));
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
