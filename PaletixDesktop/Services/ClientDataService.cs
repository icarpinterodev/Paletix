using PaletixDesktop.Models;
using PaletixDesktop.Settings;
using SharedContracts.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PaletixDesktop.Services
{
    public sealed class ClientDataService
    {
        private const string EntityName = "clients";
        private const string Endpoint = "api/Clients";
        private const string CacheKey = "clients/list/v1";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly EntityApiService<ClientsReadDto, ClientsRequestDto> _api;
        private readonly LocalDatabase _localDatabase;
        private readonly SyncQueue _syncQueue;
        private readonly SemaphoreSlim _loadSyncLock = new(1, 1);

        public ClientDataService(
            ApiClient apiClient,
            LocalDatabase localDatabase,
            SyncQueue syncQueue)
        {
            _api = new EntityApiService<ClientsReadDto, ClientsRequestDto>(apiClient, Endpoint);
            _localDatabase = localDatabase;
            _syncQueue = syncQueue;
        }

        public async Task<ClientLoadResult> LoadAsync(CancellationToken cancellationToken = default)
        {
            await _loadSyncLock.WaitAsync(cancellationToken);
            try
            {
                return await LoadInternalAsync(cancellationToken);
            }
            finally
            {
                _loadSyncLock.Release();
            }
        }

        private async Task<ClientLoadResult> LoadInternalAsync(CancellationToken cancellationToken)
        {
            await _localDatabase.InitializeAsync(cancellationToken);

            if (!await CheckConnectionAsync(cancellationToken))
            {
                return await LoadCachedAsync("Clients carregats des de SQLite. API no disponible.", cancellationToken);
            }

            var syncResult = await SynchronizePendingAsync(cancellationToken);

            try
            {
                var clients = await LoadFromApiAsync(cancellationToken);
                var cached = await ReadCacheAsync(cancellationToken);
                var active = await _syncQueue.GetActiveAsync(EntityName, cancellationToken);
                ApplyActiveOutbox(clients, cached, active);
                await SaveCacheAsync(clients, cancellationToken);

                var pending = await _syncQueue.GetPendingCountAsync(cancellationToken);
                var message = syncResult.ErrorCount > 0
                    ? $"API carregada amb {syncResult.ErrorCount} error(s) de sincronitzacio."
                    : pending > 0
                        ? $"API carregada. {pending} canvi(s) pendents."
                        : "Clients carregats des de l'API.";

                return new ClientLoadResult(clients, true, pending, message);
            }
            catch (Exception ex)
            {
                var clients = await ReadCacheAsync(cancellationToken) ?? new List<ClientRecord>();
                var active = await _syncQueue.GetActiveAsync(EntityName, cancellationToken);
                ApplyActiveOutbox(clients, clients, active);
                await SaveCacheAsync(clients, cancellationToken);

                var pending = await _syncQueue.GetPendingCountAsync(cancellationToken);
                var message = clients.Count == 0
                    ? $"No s'ha pogut carregar l'API ni SQLite: {ex.Message}"
                    : $"Clients carregats des de SQLite. {pending} canvi(s) pendents.";

                return new ClientLoadResult(clients, false, pending, message);
            }
        }

        public async Task<ClientLoadResult> LoadCachedAsync(CancellationToken cancellationToken = default)
        {
            return await LoadCachedAsync("Clients carregats des de SQLite.", cancellationToken);
        }

        public async Task<bool> CheckConnectionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(AppConstants.ConnectionCheckTimeoutSeconds));
                await _api.GetPageAsync(1, 1, timeout.Token);
                return true;
            }
            catch (ApiException)
            {
                return true;
            }
            catch (Exception ex) when (IsConnectivityFailure(ex))
            {
                return false;
            }
        }

        public async Task<ClientMutationResult> CreateAsync(
            ClientRecord draft,
            IReadOnlyList<ClientRecord> current,
            CancellationToken cancellationToken = default)
        {
            await _localDatabase.InitializeAsync(cancellationToken);
            var request = ToRequest(draft);
            var clients = current.Select(Clone).ToList();

            try
            {
                var created = await _api.CreateAsync(request, cancellationToken);
                var record = ToRecord(created, ClientSyncState.Synced, "Sincronitzat");
                clients.Insert(0, record);
                await SaveCacheAsync(clients, cancellationToken);
                return await ResultAsync(record, clients, "Client creat a l'API.", cancellationToken);
            }
            catch (Exception ex) when (IsConnectivityFailure(ex))
            {
                var record = FromRequest(NextTemporaryId(clients), request, ClientSyncState.Pending, "Creacio pendent de sincronitzar");
                clients.Insert(0, record);
                await _syncQueue.UpsertAsync(EntityName, record.IdText, "POST", Endpoint, request, "Pending", cancellationToken);
                await SaveCacheAsync(clients, cancellationToken);
                return await ResultAsync(record, clients, "Client creat offline. Queda pendent de sincronitzar.", cancellationToken);
            }
            catch (Exception ex)
            {
                var record = FromRequest(NextTemporaryId(clients), request, ClientSyncState.Error, ex.Message);
                clients.Insert(0, record);
                await _syncQueue.UpsertAsync(EntityName, record.IdText, "POST", Endpoint, request, "Error", cancellationToken);
                await SaveCacheAsync(clients, cancellationToken);
                return await ResultAsync(record, clients, "L'API ha retornat error. El client queda pendent en error sync.", cancellationToken);
            }
        }

        public async Task<ClientMutationResult> UpdateAsync(
            ClientRecord client,
            IReadOnlyList<ClientRecord> current,
            CancellationToken cancellationToken = default)
        {
            await _localDatabase.InitializeAsync(cancellationToken);
            var request = ToRequest(client);
            var clients = current.Select(Clone).ToList();
            var index = clients.FindIndex(item => item.Id == client.Id);

            if (client.Id < 0)
            {
                client.SyncState = ClientSyncState.Pending;
                client.SyncMessage = "Creacio pendent actualitzada";
                ReplaceOrAdd(clients, client);
                await _syncQueue.UpsertAsync(EntityName, client.IdText, "POST", Endpoint, request, "Pending", cancellationToken);
                await SaveCacheAsync(clients, cancellationToken);
                return await ResultAsync(client, clients, "Client pendent actualitzat offline.", cancellationToken);
            }

            try
            {
                await _api.UpdateAsync(client.Id, request, cancellationToken);
                client.SyncState = ClientSyncState.Synced;
                client.SyncMessage = "Sincronitzat";
                if (index >= 0)
                {
                    clients[index] = Clone(client);
                }

                await _syncQueue.RemoveForEntityAsync(EntityName, client.IdText, cancellationToken);
                await SaveCacheAsync(clients, cancellationToken);
                return await ResultAsync(client, clients, "Client actualitzat a l'API.", cancellationToken);
            }
            catch (Exception ex) when (IsConnectivityFailure(ex))
            {
                client.SyncState = ClientSyncState.Pending;
                client.SyncMessage = "Edicio pendent de sincronitzar";
                ReplaceOrAdd(clients, client);
                await _syncQueue.UpsertAsync(EntityName, client.IdText, "PUT", $"{Endpoint}/{client.Id}", request, "Pending", cancellationToken);
                await SaveCacheAsync(clients, cancellationToken);
                return await ResultAsync(client, clients, "Client actualitzat offline. Queda pendent de sincronitzar.", cancellationToken);
            }
            catch (Exception ex)
            {
                client.SyncState = ClientSyncState.Error;
                client.SyncMessage = ex.Message;
                ReplaceOrAdd(clients, client);
                await _syncQueue.UpsertAsync(EntityName, client.IdText, "PUT", $"{Endpoint}/{client.Id}", request, "Error", cancellationToken);
                await SaveCacheAsync(clients, cancellationToken);
                return await ResultAsync(client, clients, "L'API ha retornat error. L'edicio queda en error sync.", cancellationToken);
            }
        }

        public async Task<ClientMutationResult> DeleteAsync(
            ClientRecord client,
            IReadOnlyList<ClientRecord> current,
            CancellationToken cancellationToken = default)
        {
            await _localDatabase.InitializeAsync(cancellationToken);
            var clients = current.Select(Clone).ToList();

            if (client.Id < 0)
            {
                clients.RemoveAll(item => item.Id == client.Id);
                await _syncQueue.RemoveForEntityAsync(EntityName, client.IdText, cancellationToken);
                await SaveCacheAsync(clients, cancellationToken);
                return await ResultAsync(null, clients, "Client pendent descartat.", cancellationToken);
            }

            try
            {
                await _api.DeleteAsync(client.Id, cancellationToken);
                clients.RemoveAll(item => item.Id == client.Id);
                await _syncQueue.RemoveForEntityAsync(EntityName, client.IdText, cancellationToken);
                await SaveCacheAsync(clients, cancellationToken);
                return await ResultAsync(null, clients, "Client eliminat a l'API.", cancellationToken);
            }
            catch (Exception ex) when (IsConnectivityFailure(ex))
            {
                client.SyncState = ClientSyncState.PendingDelete;
                client.SyncMessage = "Eliminacio pendent de sincronitzar";
                ReplaceOrAdd(clients, client);
                await _syncQueue.UpsertAsync(EntityName, client.IdText, "DELETE", $"{Endpoint}/{client.Id}", new { client.Id }, "Pending", cancellationToken);
                await SaveCacheAsync(clients, cancellationToken);
                return await ResultAsync(client, clients, "Client marcat per eliminar offline.", cancellationToken);
            }
            catch (Exception ex)
            {
                client.SyncState = ClientSyncState.Error;
                client.SyncMessage = ex.Message;
                ReplaceOrAdd(clients, client);
                await _syncQueue.UpsertAsync(EntityName, client.IdText, "DELETE", $"{Endpoint}/{client.Id}", new { client.Id }, "Error", cancellationToken);
                await SaveCacheAsync(clients, cancellationToken);
                return await ResultAsync(client, clients, "L'API ha retornat error. L'eliminacio queda en error sync.", cancellationToken);
            }
        }

        private async Task<SyncProcessResult> SynchronizePendingAsync(CancellationToken cancellationToken)
        {
            var pending = await _syncQueue.GetPendingAsync(EntityName, cancellationToken);
            if (pending.Count == 0)
            {
                return new SyncProcessResult(0, 0);
            }

            var cached = await ReadCacheAsync(cancellationToken) ?? new List<ClientRecord>();
            var synced = 0;
            var errors = 0;

            foreach (var item in pending)
            {
                try
                {
                    if (item.Method == "POST")
                    {
                        var request = DeserializePayload<ClientsRequestDto>(item);
                        var created = await _api.CreateAsync(request, cancellationToken);
                        cached.RemoveAll(client => client.IdText == item.EntityId);
                        cached.Insert(0, ToRecord(created, ClientSyncState.Synced, "Sincronitzat"));
                    }
                    else if (item.Method == "PUT" && int.TryParse(item.EntityId, out var updateId))
                    {
                        var request = DeserializePayload<ClientsRequestDto>(item);
                        await _api.UpdateAsync(updateId, request, cancellationToken);
                        var index = cached.FindIndex(client => client.Id == updateId);
                        if (index >= 0)
                        {
                            cached[index] = FromRequest(updateId, request, ClientSyncState.Synced, "Sincronitzat");
                        }
                    }
                    else if (item.Method == "DELETE" && int.TryParse(item.EntityId, out var deleteId))
                    {
                        await _api.DeleteAsync(deleteId, cancellationToken);
                        cached.RemoveAll(client => client.Id == deleteId);
                    }

                    await _syncQueue.MarkCompletedAsync(item.Id, cancellationToken);
                    synced++;
                }
                catch (Exception ex) when (IsConnectivityFailure(ex))
                {
                    break;
                }
                catch (Exception)
                {
                    await _syncQueue.MarkFailedAsync(item.Id, cancellationToken);
                    MarkCachedError(cached, item);
                    errors++;
                }
            }

            await SaveCacheAsync(cached, cancellationToken);
            return new SyncProcessResult(synced, errors);
        }

        private async Task<List<ClientRecord>> LoadFromApiAsync(CancellationToken cancellationToken)
        {
            var first = await _api.GetPageAsync(1, 100, cancellationToken);
            var clients = first.Items
                .Select(item => ToRecord(item, ClientSyncState.Synced, "Sincronitzat"))
                .ToList();

            for (var page = 2; page <= first.TotalPages; page++)
            {
                var next = await _api.GetPageAsync(page, 100, cancellationToken);
                clients.AddRange(next.Items.Select(item => ToRecord(item, ClientSyncState.Synced, "Sincronitzat")));
            }

            return clients;
        }

        private async Task<ClientLoadResult> LoadCachedAsync(string message, CancellationToken cancellationToken)
        {
            await _localDatabase.InitializeAsync(cancellationToken);
            var clients = await ReadCacheAsync(cancellationToken) ?? new List<ClientRecord>();
            var active = await _syncQueue.GetActiveAsync(EntityName, cancellationToken);
            ApplyActiveOutbox(clients, clients, active);
            await SaveCacheAsync(clients, cancellationToken);

            var pending = await _syncQueue.GetPendingCountAsync(cancellationToken);
            var finalMessage = clients.Count == 0
                ? "SQLite encara no te dades locals de clients."
                : pending > 0
                    ? $"{message} {pending} canvi(s) pendents."
                    : message;

            return new ClientLoadResult(clients, false, pending, finalMessage);
        }

        private static void ApplyActiveOutbox(
            List<ClientRecord> clients,
            IReadOnlyList<ClientRecord>? cached,
            IReadOnlyList<SyncQueueItem> active)
        {
            foreach (var item in active)
            {
                var state = item.Status == "Error" ? ClientSyncState.Error : ClientSyncState.Pending;
                if (item.Method == "POST")
                {
                    var request = DeserializePayload<ClientsRequestDto>(item);
                    var id = int.TryParse(item.EntityId, out var tempId) ? tempId : -1;
                    ReplaceOrAdd(clients, FromRequest(id, request, state, item.Status == "Error" ? "Error sync" : "Creacio pendent"));
                }
                else if (item.Method == "PUT" && int.TryParse(item.EntityId, out var updateId))
                {
                    var request = DeserializePayload<ClientsRequestDto>(item);
                    ReplaceOrAdd(clients, FromRequest(updateId, request, state, item.Status == "Error" ? "Error sync" : "Edicio pendent"));
                }
                else if (item.Method == "DELETE" && int.TryParse(item.EntityId, out var deleteId))
                {
                    var existing = clients.FirstOrDefault(client => client.Id == deleteId)
                        ?? cached?.FirstOrDefault(client => client.Id == deleteId);
                    if (existing is not null)
                    {
                        existing.SyncState = item.Status == "Error" ? ClientSyncState.Error : ClientSyncState.PendingDelete;
                        existing.SyncMessage = item.Status == "Error" ? "Error sync" : "Eliminacio pendent";
                        ReplaceOrAdd(clients, existing);
                    }
                }
            }
        }

        private async Task<List<ClientRecord>?> ReadCacheAsync(CancellationToken cancellationToken)
        {
            return await _localDatabase.GetJsonAsync<List<ClientRecord>>(CacheKey, cancellationToken);
        }

        private async Task SaveCacheAsync(IReadOnlyList<ClientRecord> clients, CancellationToken cancellationToken)
        {
            var sanitized = clients.Select(Clone).ToList();
            foreach (var client in sanitized)
            {
                client.IsSelected = false;
            }

            await _localDatabase.SetJsonAsync(CacheKey, sanitized, cancellationToken);
        }

        private async Task<ClientMutationResult> ResultAsync(
            ClientRecord? client,
            IReadOnlyList<ClientRecord> clients,
            string message,
            CancellationToken cancellationToken)
        {
            var pending = await _syncQueue.GetPendingCountAsync(cancellationToken);
            return new ClientMutationResult(client, clients, pending, message);
        }

        private static ClientsRequestDto ToRequest(ClientRecord client)
        {
            return new ClientsRequestDto
            {
                NomEmpresa = string.IsNullOrWhiteSpace(client.NomEmpresa) ? "Client sense nom" : client.NomEmpresa.Trim(),
                NifEmpresa = NormalizeTaxId(client.NifEmpresa),
                Telefon = string.IsNullOrWhiteSpace(client.Telefon) ? "-" : client.Telefon.Trim(),
                Email = EmptyToNull(client.Email),
                Adreca = string.IsNullOrWhiteSpace(client.Adreca) ? "-" : client.Adreca.Trim(),
                Poblacio = string.IsNullOrWhiteSpace(client.Poblacio) ? "-" : client.Poblacio.Trim(),
                NomResponsable = EmptyToNull(client.NomResponsable)
            };
        }

        private static ClientRecord ToRecord(ClientsReadDto dto, ClientSyncState state, string message)
        {
            return new ClientRecord
            {
                Id = dto.Id,
                NomEmpresa = dto.NomEmpresa,
                NifEmpresa = dto.NifEmpresa,
                Telefon = dto.Telefon,
                Email = dto.Email,
                Adreca = dto.Adreca,
                Poblacio = dto.Poblacio,
                NomResponsable = dto.NomResponsable,
                SyncState = state,
                SyncMessage = message
            };
        }

        private static ClientRecord FromRequest(int id, ClientsRequestDto request, ClientSyncState state, string message)
        {
            return new ClientRecord
            {
                Id = id,
                NomEmpresa = request.NomEmpresa,
                NifEmpresa = request.NifEmpresa,
                Telefon = request.Telefon,
                Email = request.Email,
                Adreca = request.Adreca,
                Poblacio = request.Poblacio,
                NomResponsable = request.NomResponsable,
                SyncState = state,
                SyncMessage = message
            };
        }

        private static ClientRecord Clone(ClientRecord client)
        {
            return new ClientRecord
            {
                Id = client.Id,
                NomEmpresa = client.NomEmpresa,
                NifEmpresa = client.NifEmpresa,
                Telefon = client.Telefon,
                Email = client.Email,
                Adreca = client.Adreca,
                Poblacio = client.Poblacio,
                NomResponsable = client.NomResponsable,
                SyncState = client.SyncState,
                SyncMessage = client.SyncMessage
            };
        }

        private static T DeserializePayload<T>(SyncQueueItem item)
        {
            return JsonSerializer.Deserialize<T>(item.Payload, JsonOptions)
                ?? throw new InvalidOperationException("Payload de sincronitzacio invalid.");
        }

        private static void ReplaceOrAdd(List<ClientRecord> clients, ClientRecord client)
        {
            var index = clients.FindIndex(item => item.Id == client.Id);
            if (index >= 0)
            {
                clients[index] = Clone(client);
                return;
            }

            clients.Insert(0, Clone(client));
        }

        private static void MarkCachedError(List<ClientRecord> cached, SyncQueueItem item)
        {
            var existing = cached.FirstOrDefault(client => client.IdText == item.EntityId);
            if (existing is null)
            {
                return;
            }

            existing.SyncState = ClientSyncState.Error;
            existing.SyncMessage = "Error sync";
        }

        private static int NextTemporaryId(IReadOnlyList<ClientRecord> clients)
        {
            var min = clients.Count == 0 ? 0 : clients.Min(client => client.Id);
            return min < 0 ? min - 1 : -1;
        }

        private static bool IsConnectivityFailure(Exception ex)
        {
            return ex is HttpRequestException or TaskCanceledException;
        }

        private static string? EmptyToNull(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string? NormalizeTaxId(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim().Replace(" ", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal).ToUpperInvariant();
        }

        private sealed record SyncProcessResult(int SyncedCount, int ErrorCount);
    }

    public sealed record ClientLoadResult(
        IReadOnlyList<ClientRecord> Clients,
        bool IsOnline,
        int PendingCount,
        string Message);

    public sealed record ClientMutationResult(
        ClientRecord? Client,
        IReadOnlyList<ClientRecord> Clients,
        int PendingCount,
        string Message);
}
