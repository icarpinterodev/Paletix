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
    public sealed record OrderLoadResult(IReadOnlyList<OrderRecord> Records, bool IsOnline, int PendingCount, string Message);
    public sealed record OrderMutationResult(OrderRecord? Record, IReadOnlyList<OrderRecord> Records, int PendingCount, string Message);

    public sealed class OrderDataService
    {
        private const string EntityName = "orders";
        private const string Endpoint = "api/Comandes";
        private const string CacheKey = "orders/list/v1";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly EntityApiService<ComandesReadDto, ComandesRequestDto> _api;
        private readonly LocalDatabase _localDatabase;
        private readonly SyncQueue _syncQueue;
        private readonly LookupDataService _lookups;

        public OrderDataService(
            ApiClient apiClient,
            LocalDatabase localDatabase,
            SyncQueue syncQueue,
            LookupDataService lookups)
        {
            _api = new EntityApiService<ComandesReadDto, ComandesRequestDto>(apiClient, Endpoint);
            _localDatabase = localDatabase;
            _syncQueue = syncQueue;
            _lookups = lookups;
        }

        public async Task<OrderLoadResult> LoadAsync(CancellationToken cancellationToken = default)
        {
            await _localDatabase.InitializeAsync(cancellationToken);

            if (!await CheckConnectionAsync(cancellationToken))
            {
                return await LoadCachedAsync("Comandes carregades des de SQLite. API no disponible.", cancellationToken);
            }

            var syncResult = await SynchronizePendingAsync(cancellationToken);

            try
            {
                var records = await LoadFromApiAsync(cancellationToken);
                var cached = await ReadCacheAsync(cancellationToken);
                var active = await _syncQueue.GetActiveAsync(EntityName, cancellationToken);
                ApplyActiveOutbox(records, cached, active);
                await EnrichAsync(records, cancellationToken);
                await SaveCacheAsync(records, cancellationToken);

                var pending = await _syncQueue.GetPendingCountAsync(cancellationToken);
                var message = syncResult.ErrorCount > 0
                    ? $"API carregada amb {syncResult.ErrorCount} error(s) de sincronitzacio."
                    : pending > 0
                        ? $"API carregada. {pending} canvi(s) pendents."
                        : "Comandes carregades des de l'API.";
                return new OrderLoadResult(records, true, pending, message);
            }
            catch (Exception ex)
            {
                var records = await ReadCacheAsync(cancellationToken) ?? new List<OrderRecord>();
                var active = await _syncQueue.GetActiveAsync(EntityName, cancellationToken);
                ApplyActiveOutbox(records, records, active);
                await EnrichAsync(records, cancellationToken);
                await SaveCacheAsync(records, cancellationToken);

                var pending = await _syncQueue.GetPendingCountAsync(cancellationToken);
                var message = records.Count == 0
                    ? $"No s'ha pogut carregar l'API ni SQLite: {ex.Message}"
                    : $"Comandes carregades des de SQLite. {pending} canvi(s) pendents.";
                return new OrderLoadResult(records, false, pending, message);
            }
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

        public async Task<OrderMutationResult> CreateAsync(OrderRecord draft, IReadOnlyList<OrderRecord> current, CancellationToken cancellationToken = default)
        {
            await _localDatabase.InitializeAsync(cancellationToken);
            var request = ToRequest(draft);
            var records = current.Select(Clone).ToList();

            try
            {
                var created = await _api.CreateAsync(request, cancellationToken);
                var record = ToRecord(created, OrderSyncState.Synced, "Sincronitzat");
                records.Insert(0, record);
                await EnrichAsync(records, cancellationToken);
                await SaveCacheAsync(records, cancellationToken);
                return await ResultAsync(record, records, "Comanda creada a l'API.", cancellationToken);
            }
            catch (Exception ex) when (IsConnectivityFailure(ex))
            {
                var record = FromRequest(NextTemporaryId(records), request, OrderSyncState.Pending, "Creacio pendent de sincronitzar");
                records.Insert(0, record);
                await _syncQueue.UpsertAsync(EntityName, record.IdText, "POST", Endpoint, request, "Pending", cancellationToken);
                await EnrichAsync(records, cancellationToken);
                await SaveCacheAsync(records, cancellationToken);
                return await ResultAsync(record, records, "Comanda creada offline. Queda pendent de sincronitzar.", cancellationToken);
            }
            catch (Exception ex)
            {
                var record = FromRequest(NextTemporaryId(records), request, OrderSyncState.Error, ex.Message);
                records.Insert(0, record);
                await _syncQueue.UpsertAsync(EntityName, record.IdText, "POST", Endpoint, request, "Error", cancellationToken);
                await EnrichAsync(records, cancellationToken);
                await SaveCacheAsync(records, cancellationToken);
                return await ResultAsync(record, records, "L'API ha retornat error. La comanda queda pendent en error sync.", cancellationToken);
            }
        }

        public async Task<OrderMutationResult> UpdateAsync(OrderRecord record, IReadOnlyList<OrderRecord> current, CancellationToken cancellationToken = default)
        {
            await _localDatabase.InitializeAsync(cancellationToken);
            var request = ToRequest(record);
            var records = current.Select(Clone).ToList();

            if (record.Id < 0)
            {
                record.SyncState = OrderSyncState.Pending;
                record.SyncMessage = "Creacio pendent actualitzada";
                ReplaceOrAdd(records, record);
                await _syncQueue.UpsertAsync(EntityName, record.IdText, "POST", Endpoint, request, "Pending", cancellationToken);
                await EnrichAsync(records, cancellationToken);
                await SaveCacheAsync(records, cancellationToken);
                return await ResultAsync(record, records, "Comanda pendent actualitzada offline.", cancellationToken);
            }

            try
            {
                await _api.UpdateAsync(record.Id, request, cancellationToken);
                record.SyncState = OrderSyncState.Synced;
                record.SyncMessage = "Sincronitzat";
                ReplaceOrAdd(records, record);
                await _syncQueue.RemoveForEntityAsync(EntityName, record.IdText, cancellationToken);
                await EnrichAsync(records, cancellationToken);
                await SaveCacheAsync(records, cancellationToken);
                return await ResultAsync(record, records, "Comanda actualitzada a l'API.", cancellationToken);
            }
            catch (Exception ex) when (IsConnectivityFailure(ex))
            {
                record.SyncState = OrderSyncState.Pending;
                record.SyncMessage = "Edicio pendent de sincronitzar";
                ReplaceOrAdd(records, record);
                await _syncQueue.UpsertAsync(EntityName, record.IdText, "PUT", $"{Endpoint}/{record.Id}", request, "Pending", cancellationToken);
                await EnrichAsync(records, cancellationToken);
                await SaveCacheAsync(records, cancellationToken);
                return await ResultAsync(record, records, "Comanda actualitzada offline. Queda pendent de sincronitzar.", cancellationToken);
            }
            catch (Exception ex)
            {
                record.SyncState = OrderSyncState.Error;
                record.SyncMessage = ex.Message;
                ReplaceOrAdd(records, record);
                await _syncQueue.UpsertAsync(EntityName, record.IdText, "PUT", $"{Endpoint}/{record.Id}", request, "Error", cancellationToken);
                await EnrichAsync(records, cancellationToken);
                await SaveCacheAsync(records, cancellationToken);
                return await ResultAsync(record, records, "L'API ha retornat error. L'edicio queda en error sync.", cancellationToken);
            }
        }

        public async Task<OrderMutationResult> DeleteAsync(OrderRecord record, IReadOnlyList<OrderRecord> current, CancellationToken cancellationToken = default)
        {
            await _localDatabase.InitializeAsync(cancellationToken);
            var records = current.Select(Clone).ToList();

            if (record.Id < 0)
            {
                records.RemoveAll(item => item.Id == record.Id);
                await _syncQueue.RemoveForEntityAsync(EntityName, record.IdText, cancellationToken);
                await SaveCacheAsync(records, cancellationToken);
                return await ResultAsync(null, records, "Comanda pendent descartada.", cancellationToken);
            }

            try
            {
                await _api.DeleteAsync(record.Id, cancellationToken);
                records.RemoveAll(item => item.Id == record.Id);
                await _syncQueue.RemoveForEntityAsync(EntityName, record.IdText, cancellationToken);
                await SaveCacheAsync(records, cancellationToken);
                return await ResultAsync(null, records, "Comanda eliminada a l'API.", cancellationToken);
            }
            catch (Exception ex) when (IsConnectivityFailure(ex))
            {
                record.SyncState = OrderSyncState.PendingDelete;
                record.SyncMessage = "Eliminacio pendent de sincronitzar";
                ReplaceOrAdd(records, record);
                await _syncQueue.UpsertAsync(EntityName, record.IdText, "DELETE", $"{Endpoint}/{record.Id}", new { record.Id }, "Pending", cancellationToken);
                await SaveCacheAsync(records, cancellationToken);
                return await ResultAsync(record, records, "Comanda marcada per eliminar offline.", cancellationToken);
            }
            catch (Exception ex)
            {
                record.SyncState = OrderSyncState.Error;
                record.SyncMessage = ex.Message;
                ReplaceOrAdd(records, record);
                await _syncQueue.UpsertAsync(EntityName, record.IdText, "DELETE", $"{Endpoint}/{record.Id}", new { record.Id }, "Error", cancellationToken);
                await SaveCacheAsync(records, cancellationToken);
                return await ResultAsync(record, records, "L'API ha retornat error. L'eliminacio queda en error sync.", cancellationToken);
            }
        }

        private async Task<SyncProcessResult> SynchronizePendingAsync(CancellationToken cancellationToken)
        {
            var pending = await _syncQueue.GetPendingAsync(EntityName, cancellationToken);
            if (pending.Count == 0)
            {
                return new SyncProcessResult(0, 0);
            }

            var cached = await ReadCacheAsync(cancellationToken) ?? new List<OrderRecord>();
            var synced = 0;
            var errors = 0;

            foreach (var item in pending)
            {
                try
                {
                    if (item.Method == "POST")
                    {
                        var request = DeserializePayload<ComandesRequestDto>(item);
                        var created = await _api.CreateAsync(request, cancellationToken);
                        cached.RemoveAll(order => order.IdText == item.EntityId);
                        cached.Insert(0, ToRecord(created, OrderSyncState.Synced, "Sincronitzat"));
                    }
                    else if (item.Method == "PUT" && int.TryParse(item.EntityId, out var updateId))
                    {
                        var request = DeserializePayload<ComandesRequestDto>(item);
                        await _api.UpdateAsync(updateId, request, cancellationToken);
                        ReplaceOrAdd(cached, FromRequest(updateId, request, OrderSyncState.Synced, "Sincronitzat"));
                    }
                    else if (item.Method == "DELETE" && int.TryParse(item.EntityId, out var deleteId))
                    {
                        await _api.DeleteAsync(deleteId, cancellationToken);
                        cached.RemoveAll(order => order.Id == deleteId);
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

        private async Task<List<OrderRecord>> LoadFromApiAsync(CancellationToken cancellationToken)
        {
            var first = await _api.GetPageAsync(1, 100, cancellationToken);
            var records = first.Items.Select(item => ToRecord(item, OrderSyncState.Synced, "Sincronitzat")).ToList();
            for (var page = 2; page <= first.TotalPages; page++)
            {
                var next = await _api.GetPageAsync(page, 100, cancellationToken);
                records.AddRange(next.Items.Select(item => ToRecord(item, OrderSyncState.Synced, "Sincronitzat")));
            }

            await EnrichAsync(records, cancellationToken);
            return records;
        }

        private async Task<OrderLoadResult> LoadCachedAsync(string message, CancellationToken cancellationToken)
        {
            var records = await ReadCacheAsync(cancellationToken) ?? new List<OrderRecord>();
            var active = await _syncQueue.GetActiveAsync(EntityName, cancellationToken);
            ApplyActiveOutbox(records, records, active);
            await EnrichAsync(records, cancellationToken);
            await SaveCacheAsync(records, cancellationToken);

            var pending = await _syncQueue.GetPendingCountAsync(cancellationToken);
            var finalMessage = records.Count == 0
                ? "SQLite encara no te dades locals de comandes."
                : pending > 0
                    ? $"{message} {pending} canvi(s) pendents."
                    : message;

            return new OrderLoadResult(records, false, pending, finalMessage);
        }

        private async Task EnrichAsync(IReadOnlyList<OrderRecord> records, CancellationToken cancellationToken)
        {
            var clients = (await _lookups.GetClientsAsync(cancellationToken)).Where(item => item.Id.HasValue).ToDictionary(item => item.Id!.Value, item => item.Label);
            var users = (await _lookups.GetUsersAsync(cancellationToken)).Where(item => item.Id.HasValue).ToDictionary(item => item.Id!.Value, item => item.Label);
            var vehicles = (await _lookups.GetVehiclesAsync(cancellationToken)).Where(item => item.Id.HasValue).ToDictionary(item => item.Id!.Value, item => item.Label);
            var states = (await _lookups.GetStatesAsync(cancellationToken)).Where(item => item.Id.HasValue).ToDictionary(item => item.Id!.Value, item => item.Label);
            var products = (await _lookups.GetProductsAsync(cancellationToken)).Where(item => item.Id.HasValue).ToDictionary(item => item.Id!.Value, item => item.Label);

            foreach (var record in records)
            {
                record.ClientText = clients.TryGetValue(record.IdClient, out var client) ? client : $"Client {record.IdClient}";
                record.ChoferText = users.TryGetValue(record.IdChofer, out var chofer) ? chofer : $"Usuari {record.IdChofer}";
                record.PreparadorText = users.TryGetValue(record.IdPreparador, out var preparador) ? preparador : $"Usuari {record.IdPreparador}";
                record.VehicleText = vehicles.TryGetValue(record.IdVehicleTransportista, out var vehicle) ? vehicle : $"Vehicle {record.IdVehicleTransportista}";
                record.EstatText = states.TryGetValue(record.IdEstat, out var state) ? state : record.Estat ?? $"Estat {record.IdEstat}";

                foreach (var line in record.Lines)
                {
                    line.ProducteText = products.TryGetValue(line.IdProducte, out var product) ? product : $"Producte {line.IdProducte}";
                }
            }
        }

        private static void ApplyActiveOutbox(List<OrderRecord> records, IReadOnlyList<OrderRecord>? cached, IReadOnlyList<SyncQueueItem> active)
        {
            foreach (var item in active)
            {
                var state = item.Status == "Error" ? OrderSyncState.Error : OrderSyncState.Pending;
                if (item.Method == "POST")
                {
                    var request = DeserializePayload<ComandesRequestDto>(item);
                    var id = int.TryParse(item.EntityId, out var tempId) ? tempId : -1;
                    ReplaceOrAdd(records, FromRequest(id, request, state, item.Status == "Error" ? "Error sync" : "Creacio pendent"));
                }
                else if (item.Method == "PUT" && int.TryParse(item.EntityId, out var updateId))
                {
                    var request = DeserializePayload<ComandesRequestDto>(item);
                    ReplaceOrAdd(records, FromRequest(updateId, request, state, item.Status == "Error" ? "Error sync" : "Edicio pendent"));
                }
                else if (item.Method == "DELETE" && int.TryParse(item.EntityId, out var deleteId))
                {
                    var existing = records.FirstOrDefault(order => order.Id == deleteId)
                        ?? cached?.FirstOrDefault(order => order.Id == deleteId);
                    if (existing is not null)
                    {
                        existing.SyncState = item.Status == "Error" ? OrderSyncState.Error : OrderSyncState.PendingDelete;
                        existing.SyncMessage = item.Status == "Error" ? "Error sync" : "Eliminacio pendent";
                        ReplaceOrAdd(records, existing);
                    }
                }
            }
        }

        private async Task<List<OrderRecord>?> ReadCacheAsync(CancellationToken cancellationToken)
        {
            return await _localDatabase.GetJsonAsync<List<OrderRecord>>(CacheKey, cancellationToken);
        }

        private async Task SaveCacheAsync(IReadOnlyList<OrderRecord> records, CancellationToken cancellationToken)
        {
            var sanitized = records.Select(Clone).ToList();
            foreach (var record in sanitized)
            {
                record.IsSelected = false;
            }

            await _localDatabase.SetJsonAsync(CacheKey, sanitized, cancellationToken);
        }

        private async Task<OrderMutationResult> ResultAsync(OrderRecord? record, IReadOnlyList<OrderRecord> records, string message, CancellationToken cancellationToken)
        {
            var pending = await _syncQueue.GetPendingCountAsync(cancellationToken);
            return new OrderMutationResult(record, records, pending, message);
        }

        private static ComandesRequestDto ToRequest(OrderRecord record) => new()
        {
            IdClient = record.IdClient,
            IdChofer = record.IdChofer,
            IdPreparador = record.IdPreparador,
            IdVehicleTransportista = record.IdVehicleTransportista,
            IdEstat = record.IdEstat,
            Notes = record.Notes,
            DataPrevistaEntrega = record.DataPrevistaEntrega,
            DataEntregat = record.DataEntregat,
            PoblacioEntregaAlternativa = record.PoblacioEntregaAlternativa,
            AdrecaEntregaAlternativa = record.AdrecaEntregaAlternativa,
            Estat = record.Estat,
            Linies = record.Lines.Select(line => new ComandesLiniaRequestDto
            {
                IdProducte = line.IdProducte,
                IdUbicacio = line.IdUbicacio,
                Ubicacio = line.Ubicacio,
                Palets = line.Palets,
                Caixes = line.Caixes,
                IdEstatVerificacio = line.IdEstatVerificacio
            }).ToList()
        };

        private static OrderRecord ToRecord(ComandesReadDto dto, OrderSyncState state, string message) => new()
        {
            Id = dto.Id,
            IdClient = dto.IdClient,
            IdChofer = dto.IdChofer,
            IdPreparador = dto.IdPreparador,
            IdVehicleTransportista = dto.IdVehicleTransportista,
            IdEstat = dto.IdEstat,
            DataCreacio = dto.DataCreacio,
            Notes = dto.Notes,
            DataPrevistaEntrega = dto.DataPrevistaEntrega,
            DataEntregat = dto.DataEntregat,
            PoblacioEntregaAlternativa = dto.PoblacioEntregaAlternativa,
            AdrecaEntregaAlternativa = dto.AdrecaEntregaAlternativa,
            Estat = dto.Estat,
            Lines = dto.Linies.Select(line => new OrderLineRecord
            {
                Id = line.Id,
                IdProducte = line.IdProducte,
                IdUbicacio = line.IdUbicacio,
                Ubicacio = line.Ubicacio,
                Palets = line.Palets,
                Caixes = line.Caixes,
                IdEstatVerificacio = line.IdEstatVerificacio
            }).ToList(),
            SyncState = state,
            SyncMessage = message
        };

        private static OrderRecord FromRequest(int id, ComandesRequestDto request, OrderSyncState state, string message) => new()
        {
            Id = id,
            IdClient = request.IdClient,
            IdChofer = request.IdChofer,
            IdPreparador = request.IdPreparador,
            IdVehicleTransportista = request.IdVehicleTransportista,
            IdEstat = request.IdEstat,
            Notes = request.Notes,
            DataPrevistaEntrega = request.DataPrevistaEntrega,
            DataEntregat = request.DataEntregat,
            PoblacioEntregaAlternativa = request.PoblacioEntregaAlternativa,
            AdrecaEntregaAlternativa = request.AdrecaEntregaAlternativa,
            Estat = request.Estat,
            Lines = request.Linies.Select((line, index) => new OrderLineRecord
            {
                Id = index + 1,
                IdProducte = line.IdProducte,
                IdUbicacio = line.IdUbicacio,
                Ubicacio = line.Ubicacio,
                Palets = line.Palets,
                Caixes = line.Caixes,
                IdEstatVerificacio = line.IdEstatVerificacio
            }).ToList(),
            SyncState = state,
            SyncMessage = message
        };

        private static OrderRecord Clone(OrderRecord record) => new()
        {
            Id = record.Id,
            IdClient = record.IdClient,
            IdChofer = record.IdChofer,
            IdPreparador = record.IdPreparador,
            IdVehicleTransportista = record.IdVehicleTransportista,
            IdEstat = record.IdEstat,
            DataCreacio = record.DataCreacio,
            Notes = record.Notes,
            DataPrevistaEntrega = record.DataPrevistaEntrega,
            DataEntregat = record.DataEntregat,
            PoblacioEntregaAlternativa = record.PoblacioEntregaAlternativa,
            AdrecaEntregaAlternativa = record.AdrecaEntregaAlternativa,
            Estat = record.Estat,
            ClientText = record.ClientText,
            ChoferText = record.ChoferText,
            PreparadorText = record.PreparadorText,
            VehicleText = record.VehicleText,
            EstatText = record.EstatText,
            Lines = record.Lines.Select(line => new OrderLineRecord
            {
                Id = line.Id,
                IdProducte = line.IdProducte,
                ProducteText = line.ProducteText,
                IdUbicacio = line.IdUbicacio,
                Ubicacio = line.Ubicacio,
                Palets = line.Palets,
                Caixes = line.Caixes,
                IdEstatVerificacio = line.IdEstatVerificacio
            }).ToList(),
            SyncState = record.SyncState,
            SyncMessage = record.SyncMessage
        };

        private static void ReplaceOrAdd(List<OrderRecord> records, OrderRecord record)
        {
            var index = records.FindIndex(item => item.Id == record.Id);
            if (index >= 0)
            {
                records[index] = Clone(record);
            }
            else
            {
                records.Insert(0, Clone(record));
            }
        }

        private static void MarkCachedError(List<OrderRecord> cached, SyncQueueItem item)
        {
            var existing = cached.FirstOrDefault(record => record.IdText == item.EntityId);
            if (existing is not null)
            {
                existing.SyncState = OrderSyncState.Error;
                existing.SyncMessage = "Error sync";
            }
        }

        private static int NextTemporaryId(IReadOnlyList<OrderRecord> records)
        {
            var min = records.Count == 0 ? 0 : records.Min(record => record.Id);
            return min < 0 ? min - 1 : -1;
        }

        private static T DeserializePayload<T>(SyncQueueItem item)
        {
            return JsonSerializer.Deserialize<T>(item.Payload, JsonOptions)
                ?? throw new InvalidOperationException("Payload de comandes invalid.");
        }

        private static bool IsConnectivityFailure(Exception ex)
        {
            return ex is HttpRequestException or TaskCanceledException;
        }

        private sealed record SyncProcessResult(int SyncedCount, int ErrorCount);
    }
}
