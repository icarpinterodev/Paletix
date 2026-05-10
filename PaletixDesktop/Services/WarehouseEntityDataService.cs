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
    public sealed record WarehouseLoadResult<TRecord>(
        IReadOnlyList<TRecord> Records,
        bool IsOnline,
        int PendingCount,
        string Message)
        where TRecord : IWarehouseRecord;

    public sealed record WarehouseMutationResult<TRecord>(
        TRecord? Record,
        IReadOnlyList<TRecord> Records,
        int PendingCount,
        string Message)
        where TRecord : IWarehouseRecord;

    public interface IWarehouseDataService<TRecord>
        where TRecord : IWarehouseRecord
    {
        Task<WarehouseLoadResult<TRecord>> LoadAsync(CancellationToken cancellationToken = default);
        Task<WarehouseLoadResult<TRecord>> LoadCachedAsync(CancellationToken cancellationToken = default);
        Task<bool> CheckConnectionAsync(CancellationToken cancellationToken = default);
        Task<WarehouseMutationResult<TRecord>> CreateAsync(TRecord draft, IReadOnlyList<TRecord> current, CancellationToken cancellationToken = default);
        Task<WarehouseMutationResult<TRecord>> UpdateAsync(TRecord record, IReadOnlyList<TRecord> current, CancellationToken cancellationToken = default);
        Task<WarehouseMutationResult<TRecord>> DeleteAsync(TRecord record, IReadOnlyList<TRecord> current, CancellationToken cancellationToken = default);
    }

    public class WarehouseEntityDataService<TRecord, TRead, TRequest>
        : IWarehouseDataService<TRecord>
        where TRecord : class, IWarehouseRecord
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly string _entityName;
        private readonly string _entityLabel;
        private readonly string _endpoint;
        private readonly string _cacheKey;
        private readonly ApiClient _apiClient;
        private readonly EntityApiService<TRead, TRequest> _api;
        private readonly LocalDatabase _localDatabase;
        private readonly SyncQueue _syncQueue;
        private readonly Func<TRecord, TRequest> _toRequest;
        private readonly Func<TRead, WarehouseSyncState, string, TRecord> _toRecord;
        private readonly Func<int, TRequest, WarehouseSyncState, string, TRecord> _fromRequest;
        private readonly Func<TRecord, TRecord> _clone;

        public WarehouseEntityDataService(
            ApiClient apiClient,
            LocalDatabase localDatabase,
            SyncQueue syncQueue,
            string entityName,
            string entityLabel,
            string endpoint,
            string cacheKey,
            Func<TRecord, TRequest> toRequest,
            Func<TRead, WarehouseSyncState, string, TRecord> toRecord,
            Func<int, TRequest, WarehouseSyncState, string, TRecord> fromRequest,
            Func<TRecord, TRecord> clone)
        {
            _entityName = entityName;
            _entityLabel = entityLabel;
            _endpoint = endpoint;
            _cacheKey = cacheKey;
            _apiClient = apiClient;
            _api = new EntityApiService<TRead, TRequest>(apiClient, endpoint);
            _localDatabase = localDatabase;
            _syncQueue = syncQueue;
            _toRequest = toRequest;
            _toRecord = toRecord;
            _fromRequest = fromRequest;
            _clone = clone;
        }

        public async Task<WarehouseLoadResult<TRecord>> LoadAsync(CancellationToken cancellationToken = default)
        {
            await _localDatabase.InitializeAsync(cancellationToken);

            if (!await CheckConnectionAsync(cancellationToken))
            {
                return await LoadCachedAsync($"{_entityLabel} carregat des de SQLite. API no disponible.", cancellationToken);
            }

            await SynchronizeAdditionalPendingAsync(cancellationToken);
            var syncResult = await SynchronizePendingAsync(cancellationToken);

            try
            {
                var records = await LoadFromApiAsync(cancellationToken);
                var cached = await ReadCacheAsync(cancellationToken);
                var active = await _syncQueue.GetActiveAsync(_entityName, cancellationToken);
                ApplyActiveOutbox(records, cached, active);
                await SaveCacheAsync(records, cancellationToken);

                var pending = await _syncQueue.GetPendingCountAsync(cancellationToken);
                var message = syncResult.ErrorCount > 0
                    ? $"API carregada amb {syncResult.ErrorCount} error(s) de sincronitzacio."
                    : pending > 0
                        ? $"API carregada. {pending} canvi(s) pendents."
                        : $"{_entityLabel} carregat des de l'API.";

                return new WarehouseLoadResult<TRecord>(records, true, pending, message);
            }
            catch (Exception ex)
            {
                var records = await ReadCacheAsync(cancellationToken) ?? new List<TRecord>();
                var active = await _syncQueue.GetActiveAsync(_entityName, cancellationToken);
                ApplyActiveOutbox(records, records, active);
                await SaveCacheAsync(records, cancellationToken);

                var pending = await _syncQueue.GetPendingCountAsync(cancellationToken);
                var message = records.Count == 0
                    ? $"No s'ha pogut carregar l'API ni SQLite: {ex.Message}"
                    : $"{_entityLabel} carregat des de SQLite. {pending} canvi(s) pendents.";

                return new WarehouseLoadResult<TRecord>(records, false, pending, message);
            }
        }

        public async Task<WarehouseLoadResult<TRecord>> LoadCachedAsync(CancellationToken cancellationToken = default)
        {
            return await LoadCachedAsync($"{_entityLabel} carregat des de SQLite.", cancellationToken);
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

        public async Task<WarehouseMutationResult<TRecord>> CreateAsync(
            TRecord draft,
            IReadOnlyList<TRecord> current,
            CancellationToken cancellationToken = default)
        {
            await _localDatabase.InitializeAsync(cancellationToken);
            var request = _toRequest(draft);
            var records = current.Select(_clone).ToList();

            try
            {
                var created = await _api.CreateAsync(request, cancellationToken);
                var record = _toRecord(created, WarehouseSyncState.Synced, "Sincronitzat");
                records.Insert(0, record);
                await SaveCacheAsync(records, cancellationToken);
                return await ResultAsync(record, records, $"{_entityLabel} creat a l'API.", cancellationToken);
            }
            catch (Exception ex) when (IsConnectivityFailure(ex))
            {
                var record = _fromRequest(NextTemporaryId(records), request, WarehouseSyncState.Pending, "Creacio pendent de sincronitzar");
                records.Insert(0, record);
                await _syncQueue.UpsertAsync(_entityName, record.IdText, "POST", _endpoint, request!, "Pending", cancellationToken);
                await SaveCacheAsync(records, cancellationToken);
                return await ResultAsync(record, records, $"{_entityLabel} creat offline. Queda pendent de sincronitzar.", cancellationToken);
            }
            catch (Exception ex)
            {
                var record = _fromRequest(NextTemporaryId(records), request, WarehouseSyncState.Error, ex.Message);
                records.Insert(0, record);
                await _syncQueue.UpsertAsync(_entityName, record.IdText, "POST", _endpoint, request!, "Error", cancellationToken);
                await SaveCacheAsync(records, cancellationToken);
                return await ResultAsync(record, records, $"L'API ha retornat error. {_entityLabel} queda pendent en error sync.", cancellationToken);
            }
        }

        public async Task<WarehouseMutationResult<TRecord>> UpdateAsync(
            TRecord record,
            IReadOnlyList<TRecord> current,
            CancellationToken cancellationToken = default)
        {
            await _localDatabase.InitializeAsync(cancellationToken);
            var request = _toRequest(record);
            var records = current.Select(_clone).ToList();

            if (record.Id < 0)
            {
                record.SyncState = WarehouseSyncState.Pending;
                record.SyncMessage = "Creacio pendent actualitzada";
                ReplaceOrAdd(records, record);
                await _syncQueue.UpsertAsync(_entityName, record.IdText, "POST", _endpoint, request!, "Pending", cancellationToken);
                await SaveCacheAsync(records, cancellationToken);
                return await ResultAsync(record, records, $"{_entityLabel} pendent actualitzat offline.", cancellationToken);
            }

            try
            {
                await _api.UpdateAsync(record.Id, request, cancellationToken);
                record.SyncState = WarehouseSyncState.Synced;
                record.SyncMessage = "Sincronitzat";
                ReplaceOrAdd(records, record);
                await _syncQueue.RemoveForEntityAsync(_entityName, record.IdText, cancellationToken);
                await SaveCacheAsync(records, cancellationToken);
                return await ResultAsync(record, records, $"{_entityLabel} actualitzat a l'API.", cancellationToken);
            }
            catch (Exception ex) when (IsConnectivityFailure(ex))
            {
                record.SyncState = WarehouseSyncState.Pending;
                record.SyncMessage = "Edicio pendent de sincronitzar";
                ReplaceOrAdd(records, record);
                await _syncQueue.UpsertAsync(_entityName, record.IdText, "PUT", $"{_endpoint}/{record.Id}", request!, "Pending", cancellationToken);
                await SaveCacheAsync(records, cancellationToken);
                return await ResultAsync(record, records, $"{_entityLabel} actualitzat offline. Queda pendent de sincronitzar.", cancellationToken);
            }
            catch (Exception ex)
            {
                record.SyncState = WarehouseSyncState.Error;
                record.SyncMessage = ex.Message;
                ReplaceOrAdd(records, record);
                await _syncQueue.UpsertAsync(_entityName, record.IdText, "PUT", $"{_endpoint}/{record.Id}", request!, "Error", cancellationToken);
                await SaveCacheAsync(records, cancellationToken);
                return await ResultAsync(record, records, $"L'API ha retornat error. L'edicio queda en error sync.", cancellationToken);
            }
        }

        public async Task<WarehouseMutationResult<TRecord>> DeleteAsync(
            TRecord record,
            IReadOnlyList<TRecord> current,
            CancellationToken cancellationToken = default)
        {
            await _localDatabase.InitializeAsync(cancellationToken);
            var records = current.Select(_clone).ToList();

            if (record.Id < 0)
            {
                records.RemoveAll(item => item.Id == record.Id);
                await _syncQueue.RemoveForEntityAsync(_entityName, record.IdText, cancellationToken);
                await SaveCacheAsync(records, cancellationToken);
                return await ResultAsync(null, records, $"{_entityLabel} pendent descartat.", cancellationToken);
            }

            try
            {
                await _api.DeleteAsync(record.Id, cancellationToken);
                records.RemoveAll(item => item.Id == record.Id);
                await _syncQueue.RemoveForEntityAsync(_entityName, record.IdText, cancellationToken);
                await SaveCacheAsync(records, cancellationToken);
                return await ResultAsync(null, records, $"{_entityLabel} eliminat a l'API.", cancellationToken);
            }
            catch (Exception ex) when (IsConnectivityFailure(ex))
            {
                record.SyncState = WarehouseSyncState.PendingDelete;
                record.SyncMessage = "Eliminacio pendent de sincronitzar";
                ReplaceOrAdd(records, record);
                await _syncQueue.UpsertAsync(_entityName, record.IdText, "DELETE", $"{_endpoint}/{record.Id}", new { record.Id }, "Pending", cancellationToken);
                await SaveCacheAsync(records, cancellationToken);
                return await ResultAsync(record, records, $"{_entityLabel} marcat per eliminar offline.", cancellationToken);
            }
            catch (Exception ex)
            {
                record.SyncState = WarehouseSyncState.Error;
                record.SyncMessage = ex.Message;
                ReplaceOrAdd(records, record);
                await _syncQueue.UpsertAsync(_entityName, record.IdText, "DELETE", $"{_endpoint}/{record.Id}", new { record.Id }, "Error", cancellationToken);
                await SaveCacheAsync(records, cancellationToken);
                return await ResultAsync(record, records, $"L'API ha retornat error. L'eliminacio queda en error sync.", cancellationToken);
            }
        }

        protected async Task<WarehouseMutationResult<TRecord>> CreateManyAsync(
            IReadOnlyList<TRecord> drafts,
            IReadOnlyList<TRecord> current,
            string bulkEndpoint,
            CancellationToken cancellationToken = default)
        {
            await _localDatabase.InitializeAsync(cancellationToken);
            var records = current.Select(_clone).ToList();

            if (drafts.Count == 0)
            {
                return await ResultAsync(null, records, $"No hi ha {_entityLabel.ToLowerInvariant()} noves per crear.", cancellationToken);
            }

            var requests = drafts.Select(_toRequest).ToList();

            try
            {
                var created = await _apiClient.PostAsync<IReadOnlyList<TRequest>, List<TRead>>(bulkEndpoint, requests, cancellationToken);
                var createdRecords = created
                    .Select(item => _toRecord(item, WarehouseSyncState.Synced, "Sincronitzat"))
                    .ToList();

                records.InsertRange(0, createdRecords);
                await SaveCacheAsync(records, cancellationToken);
                return await ResultAsync(createdRecords.FirstOrDefault(), records, $"{createdRecords.Count} {_entityLabel.ToLowerInvariant()} creades a l'API.", cancellationToken);
            }
            catch (Exception ex) when (IsConnectivityFailure(ex))
            {
                var nextId = NextTemporaryId(records);
                TRecord? firstRecord = null;
                foreach (var request in requests)
                {
                    var record = _fromRequest(nextId, request, WarehouseSyncState.Pending, "Creacio pendent de sincronitzar");
                    firstRecord ??= record;
                    records.Insert(0, record);
                    await _syncQueue.UpsertAsync(_entityName, record.IdText, "POST", _endpoint, request!, "Pending", cancellationToken);
                    nextId--;
                }

                await SaveCacheAsync(records, cancellationToken);
                return await ResultAsync(firstRecord, records, $"{requests.Count} {_entityLabel.ToLowerInvariant()} creades offline. Queden pendents de sincronitzar.", cancellationToken);
            }
            catch (Exception ex)
            {
                var nextId = NextTemporaryId(records);
                TRecord? firstRecord = null;
                foreach (var request in requests)
                {
                    var record = _fromRequest(nextId, request, WarehouseSyncState.Error, ex.Message);
                    firstRecord ??= record;
                    records.Insert(0, record);
                    await _syncQueue.UpsertAsync(_entityName, record.IdText, "POST", _endpoint, request!, "Error", cancellationToken);
                    nextId--;
                }

                await SaveCacheAsync(records, cancellationToken);
                return await ResultAsync(firstRecord, records, $"L'API ha retornat error. {requests.Count} {_entityLabel.ToLowerInvariant()} queden en error sync.", cancellationToken);
            }
        }

        private async Task<SyncProcessResult> SynchronizePendingAsync(CancellationToken cancellationToken)
        {
            var pending = await _syncQueue.GetPendingAsync(_entityName, cancellationToken);
            if (pending.Count == 0)
            {
                return new SyncProcessResult(0, 0);
            }

            var cached = await ReadCacheAsync(cancellationToken) ?? new List<TRecord>();
            var synced = 0;
            var errors = 0;

            foreach (var item in pending)
            {
                try
                {
                    if (item.Method == "POST")
                    {
                        var request = DeserializePayload<TRequest>(item);
                        var created = await _api.CreateAsync(request, cancellationToken);
                        cached.RemoveAll(record => record.IdText == item.EntityId);
                        cached.Insert(0, _toRecord(created, WarehouseSyncState.Synced, "Sincronitzat"));
                    }
                    else if (item.Method == "PUT" && int.TryParse(item.EntityId, out var updateId))
                    {
                        var request = DeserializePayload<TRequest>(item);
                        await _api.UpdateAsync(updateId, request, cancellationToken);
                        ReplaceOrAdd(cached, _fromRequest(updateId, request, WarehouseSyncState.Synced, "Sincronitzat"));
                    }
                    else if (item.Method == "DELETE" && int.TryParse(item.EntityId, out var deleteId))
                    {
                        await _api.DeleteAsync(deleteId, cancellationToken);
                        cached.RemoveAll(record => record.Id == deleteId);
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

        private async Task<List<TRecord>> LoadFromApiAsync(CancellationToken cancellationToken)
        {
            var first = await _api.GetPageAsync(1, 100, cancellationToken);
            var records = first.Items
                .Select(item => _toRecord(item, WarehouseSyncState.Synced, "Sincronitzat"))
                .ToList();

            for (var page = 2; page <= first.TotalPages; page++)
            {
                var next = await _api.GetPageAsync(page, 100, cancellationToken);
                records.AddRange(next.Items.Select(item => _toRecord(item, WarehouseSyncState.Synced, "Sincronitzat")));
            }

            return records;
        }

        private async Task<WarehouseLoadResult<TRecord>> LoadCachedAsync(string message, CancellationToken cancellationToken)
        {
            await _localDatabase.InitializeAsync(cancellationToken);
            var records = await ReadCacheAsync(cancellationToken) ?? new List<TRecord>();
            var active = await _syncQueue.GetActiveAsync(_entityName, cancellationToken);
            ApplyActiveOutbox(records, records, active);
            await SaveCacheAsync(records, cancellationToken);

            var pending = await _syncQueue.GetPendingCountAsync(cancellationToken);
            var finalMessage = records.Count == 0
                ? $"SQLite encara no te dades locals de {_entityLabel.ToLowerInvariant()}."
                : pending > 0
                    ? $"{message} {pending} canvi(s) pendents."
                    : message;

            return new WarehouseLoadResult<TRecord>(records, false, pending, finalMessage);
        }

        private void ApplyActiveOutbox(
            List<TRecord> records,
            IReadOnlyList<TRecord>? cached,
            IReadOnlyList<SyncQueueItem> active)
        {
            foreach (var item in active)
            {
                var state = item.Status == "Error" ? WarehouseSyncState.Error : WarehouseSyncState.Pending;
                var message = item.Status == "Error" ? "Error sync" : item.Method == "POST" ? "Creacio pendent" : "Edicio pendent";

                if (item.Method == "POST")
                {
                    var request = DeserializePayload<TRequest>(item);
                    var id = int.TryParse(item.EntityId, out var tempId) ? tempId : -1;
                    ReplaceOrAdd(records, _fromRequest(id, request, state, message));
                }
                else if (item.Method == "PUT" && int.TryParse(item.EntityId, out var updateId))
                {
                    var request = DeserializePayload<TRequest>(item);
                    ReplaceOrAdd(records, _fromRequest(updateId, request, state, message));
                }
                else if (item.Method == "DELETE" && int.TryParse(item.EntityId, out var deleteId))
                {
                    var existing = records.FirstOrDefault(record => record.Id == deleteId)
                        ?? cached?.FirstOrDefault(record => record.Id == deleteId);
                    if (existing is not null)
                    {
                        existing.SyncState = item.Status == "Error" ? WarehouseSyncState.Error : WarehouseSyncState.PendingDelete;
                        existing.SyncMessage = item.Status == "Error" ? "Error sync" : "Eliminacio pendent";
                        ReplaceOrAdd(records, existing);
                    }
                }
            }
        }

        protected virtual Task SynchronizeAdditionalPendingAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected async Task<List<TRecord>?> ReadCacheAsync(CancellationToken cancellationToken)
        {
            return await _localDatabase.GetJsonAsync<List<TRecord>>(_cacheKey, cancellationToken);
        }

        protected async Task SaveCacheAsync(IReadOnlyList<TRecord> records, CancellationToken cancellationToken)
        {
            var sanitized = records.Select(_clone).ToList();
            foreach (var record in sanitized)
            {
                record.IsSelected = false;
            }

            await _localDatabase.SetJsonAsync(_cacheKey, sanitized, cancellationToken);
        }

        protected async Task<WarehouseMutationResult<TRecord>> ResultAsync(
            TRecord? record,
            IReadOnlyList<TRecord> records,
            string message,
            CancellationToken cancellationToken)
        {
            var pending = await _syncQueue.GetPendingCountAsync(cancellationToken);
            return new WarehouseMutationResult<TRecord>(record, records, pending, message);
        }

        private static T DeserializePayload<T>(SyncQueueItem item)
        {
            return JsonSerializer.Deserialize<T>(item.Payload, JsonOptions)
                ?? throw new InvalidOperationException("Payload de sincronitzacio invalid.");
        }

        protected void ReplaceOrAdd(List<TRecord> records, TRecord record)
        {
            var index = records.FindIndex(item => item.Id == record.Id);
            if (index >= 0)
            {
                records[index] = _clone(record);
                return;
            }

            records.Insert(0, _clone(record));
        }

        private static void MarkCachedError(List<TRecord> cached, SyncQueueItem item)
        {
            var existing = cached.FirstOrDefault(record => record.IdText == item.EntityId);
            if (existing is null)
            {
                return;
            }

            existing.SyncState = WarehouseSyncState.Error;
            existing.SyncMessage = "Error sync";
        }

        private static int NextTemporaryId(IReadOnlyList<TRecord> records)
        {
            var min = records.Count == 0 ? 0 : records.Min(record => record.Id);
            return min < 0 ? min - 1 : -1;
        }

        protected static bool IsConnectivityFailure(Exception ex)
        {
            return ex is HttpRequestException or TaskCanceledException;
        }

        private sealed record SyncProcessResult(int SyncedCount, int ErrorCount);
    }

    public sealed class StockDataService : WarehouseEntityDataService<StockRecord, StockReadDto, StockRequestDto>
    {
        private static readonly JsonSerializerOptions StockJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly ApiClient _apiClient;
        private readonly SyncQueue _syncQueue;

        public StockDataService(ApiClient apiClient, LocalDatabase localDatabase, SyncQueue syncQueue)
            : base(apiClient, localDatabase, syncQueue, "stock", "Stock", "api/Stocks", "warehouse/stock/list/v1", ToRequest, ToRecord, FromRequest, Clone)
        {
            _apiClient = apiClient;
            _syncQueue = syncQueue;
        }

        public async Task<WarehouseMutationResult<StockRecord>> ApplyEntradaAsync(
            StockEntradaRequestDto request,
            IReadOnlyList<StockRecord> current,
            CancellationToken cancellationToken = default)
        {
            ValidatePositive(request.Quantitat);
            return await ApplyOperationAsync("Entrada", "api/Stocks/entrada", request, current, records =>
            {
                var record = FindByKey(records, request.IdProducte, request.IdUbicacio, request.IdLot);
                if (record is null)
                {
                    record = new StockRecord
                    {
                        Id = NextTemporaryStockId(records),
                        IdProducte = request.IdProducte,
                        IdUbicacio = request.IdUbicacio,
                        IdLot = request.IdLot,
                        TotalsEnStock = request.Quantitat,
                        ReservatsPerComandes = 0,
                        Disponibles = request.Quantitat,
                        SyncState = WarehouseSyncState.Pending,
                        SyncMessage = "Entrada pendent de sincronitzar"
                    };
                    records.Insert(0, record);
                }
                else
                {
                    record.TotalsEnStock += request.Quantitat;
                    record.Disponibles = Math.Max(0, record.TotalsEnStock - record.ReservatsPerComandes);
                    record.SyncState = WarehouseSyncState.Pending;
                    record.SyncMessage = "Entrada pendent de sincronitzar";
                }

                return record;
            }, cancellationToken);
        }

        public async Task<IReadOnlyList<StockMovementRecord>> LoadMovimentsAsync(CancellationToken cancellationToken = default)
        {
            var page = await _apiClient.GetPagedAsync<StockMovimentReadDto>("api/StockMoviments", 1, 100, cancellationToken);
            return page.Items.Select(ToMovementRecord).ToList();
        }

        public async Task<WarehouseMutationResult<StockRecord>> ApplyMovimentAsync(
            StockMovimentRequestDto request,
            IReadOnlyList<StockRecord> current,
            CancellationToken cancellationToken = default)
        {
            ValidatePositive(request.Quantitat);
            return await ApplyOperationAsync("Moviment", "api/Stocks/moure", request, current, records =>
            {
                var origen = FindByKey(records, request.IdProducte, request.IdUbicacioOrigen, request.IdLot)
                    ?? throw new InvalidOperationException("No existeix stock a la ubicacio origen.");
                if (Available(origen) < request.Quantitat)
                {
                    throw new InvalidOperationException("No hi ha prou stock disponible per moure.");
                }

                origen.TotalsEnStock -= request.Quantitat;
                origen.Disponibles = Math.Max(0, origen.TotalsEnStock - origen.ReservatsPerComandes);
                origen.SyncState = WarehouseSyncState.Pending;
                origen.SyncMessage = "Moviment pendent de sincronitzar";

                var desti = FindByKey(records, request.IdProducte, request.IdUbicacioDesti, request.IdLot);
                if (desti is null)
                {
                    desti = new StockRecord
                    {
                        Id = NextTemporaryStockId(records),
                        IdProducte = request.IdProducte,
                        IdUbicacio = request.IdUbicacioDesti,
                        IdLot = request.IdLot,
                        TotalsEnStock = request.Quantitat,
                        ReservatsPerComandes = 0,
                        Disponibles = request.Quantitat,
                        SyncState = WarehouseSyncState.Pending,
                        SyncMessage = "Moviment pendent de sincronitzar"
                    };
                    records.Insert(0, desti);
                }
                else
                {
                    desti.TotalsEnStock += request.Quantitat;
                    desti.Disponibles = Math.Max(0, desti.TotalsEnStock - desti.ReservatsPerComandes);
                    desti.SyncState = WarehouseSyncState.Pending;
                    desti.SyncMessage = "Moviment pendent de sincronitzar";
                }

                return desti;
            }, cancellationToken);
        }

        public Task<WarehouseMutationResult<StockRecord>> ApplyAjustAsync(StockAjustRequestDto request, IReadOnlyList<StockRecord> current, CancellationToken cancellationToken = default)
        {
            return ApplySelectedOperationAsync("Ajust", "api/Stocks/ajust", request.IdStock, request, current, record =>
            {
                if (request.NouTotal < record.ReservatsPerComandes)
                {
                    throw new InvalidOperationException("El total no pot ser inferior al stock reservat.");
                }

                record.TotalsEnStock = request.NouTotal;
                record.Disponibles = Math.Max(0, record.TotalsEnStock - record.ReservatsPerComandes);
            }, cancellationToken);
        }

        public Task<WarehouseMutationResult<StockRecord>> ApplyReservaAsync(StockReservaRequestDto request, IReadOnlyList<StockRecord> current, CancellationToken cancellationToken = default)
        {
            ValidatePositive(request.Quantitat);
            return ApplySelectedOperationAsync("Reserva", "api/Stocks/reservar", request.IdStock, request, current, record =>
            {
                if (Available(record) < request.Quantitat)
                {
                    throw new InvalidOperationException("No hi ha prou stock disponible per reservar.");
                }

                record.ReservatsPerComandes += request.Quantitat;
                record.Disponibles = Math.Max(0, record.TotalsEnStock - record.ReservatsPerComandes);
            }, cancellationToken);
        }

        public Task<WarehouseMutationResult<StockRecord>> ApplyAlliberamentAsync(StockAlliberamentRequestDto request, IReadOnlyList<StockRecord> current, CancellationToken cancellationToken = default)
        {
            ValidatePositive(request.Quantitat);
            return ApplySelectedOperationAsync("Alliberament", "api/Stocks/alliberar", request.IdStock, request, current, record =>
            {
                if (record.ReservatsPerComandes < request.Quantitat)
                {
                    throw new InvalidOperationException("No es pot alliberar mes stock del reservat.");
                }

                record.ReservatsPerComandes -= request.Quantitat;
                record.Disponibles = Math.Max(0, record.TotalsEnStock - record.ReservatsPerComandes);
            }, cancellationToken);
        }

        protected override async Task SynchronizeAdditionalPendingAsync(CancellationToken cancellationToken)
        {
            var pending = await _syncQueue.GetPendingAsync("stock_operations", cancellationToken);
            foreach (var item in pending)
            {
                try
                {
                    await PostQueuedOperationAsync(item, cancellationToken);
                    await _syncQueue.MarkCompletedAsync(item.Id, cancellationToken);
                }
                catch (Exception ex) when (IsConnectivityFailure(ex))
                {
                    break;
                }
                catch (Exception)
                {
                    await _syncQueue.MarkFailedAsync(item.Id, cancellationToken);
                }
            }
        }

        private async Task<WarehouseMutationResult<StockRecord>> ApplySelectedOperationAsync<TRequest>(
            string operation,
            string endpoint,
            int idStock,
            TRequest request,
            IReadOnlyList<StockRecord> current,
            Action<StockRecord> mutate,
            CancellationToken cancellationToken)
        {
            return await ApplyOperationAsync(operation, endpoint, request, current, records =>
            {
                var record = records.FirstOrDefault(item => item.Id == idStock)
                    ?? throw new InvalidOperationException("Selecciona un registre de stock valid.");
                mutate(record);
                record.SyncState = WarehouseSyncState.Pending;
                record.SyncMessage = $"{operation} pendent de sincronitzar";
                return record;
            }, cancellationToken);
        }

        private async Task<WarehouseMutationResult<StockRecord>> ApplyOperationAsync<TRequest>(
            string operation,
            string endpoint,
            TRequest request,
            IReadOnlyList<StockRecord> current,
            Func<List<StockRecord>, StockRecord> offlineMutation,
            CancellationToken cancellationToken)
        {
            var records = current.Select(Clone).ToList();
            try
            {
                var result = await _apiClient.PostAsync<TRequest, StockOperationResultDto>(endpoint, request, cancellationToken);
                ApplyOperationResult(records, result);
                await SaveCacheAsync(records, cancellationToken);
                return await ResultAsync(result.Desti is not null ? ToRecord(result.Desti, WarehouseSyncState.Synced, "Sincronitzat") : result.Stock is not null ? ToRecord(result.Stock, WarehouseSyncState.Synced, "Sincronitzat") : null, records, $"{operation} aplicada a l'API.", cancellationToken);
            }
            catch (Exception ex) when (IsConnectivityFailure(ex))
            {
                var record = offlineMutation(records);
                await _syncQueue.EnqueueAsync("stock_operations", null, operation, endpoint, request!, cancellationToken);
                await SaveCacheAsync(records, cancellationToken);
                return await ResultAsync(record, records, $"{operation} aplicada offline. Queda pendent de sincronitzar.", cancellationToken);
            }
            catch (Exception ex)
            {
                var record = offlineMutation(records);
                record.SyncState = WarehouseSyncState.Error;
                record.SyncMessage = ex.Message;
                await _syncQueue.EnqueueAsync("stock_operations", null, operation, endpoint, request!, cancellationToken);
                await SaveCacheAsync(records, cancellationToken);
                return await ResultAsync(record, records, $"{operation} queda en error sync.", cancellationToken);
            }
        }

        private void ApplyOperationResult(List<StockRecord> records, StockOperationResultDto result)
        {
            if (result.Stock is not null)
            {
                ReplaceOrAdd(records, ToRecord(result.Stock, WarehouseSyncState.Synced, "Sincronitzat"));
            }

            if (result.Origen is not null)
            {
                ReplaceOrAdd(records, ToRecord(result.Origen, WarehouseSyncState.Synced, "Sincronitzat"));
            }

            if (result.Desti is not null)
            {
                ReplaceOrAdd(records, ToRecord(result.Desti, WarehouseSyncState.Synced, "Sincronitzat"));
            }
        }

        private async Task PostQueuedOperationAsync(SyncQueueItem item, CancellationToken cancellationToken)
        {
            if (item.Method == "Entrada")
            {
                await _apiClient.PostAsync<StockEntradaRequestDto, StockOperationResultDto>(item.Endpoint, Deserialize<StockEntradaRequestDto>(item), cancellationToken);
            }
            else if (item.Method == "Moviment")
            {
                await _apiClient.PostAsync<StockMovimentRequestDto, StockOperationResultDto>(item.Endpoint, Deserialize<StockMovimentRequestDto>(item), cancellationToken);
            }
            else if (item.Method == "Ajust")
            {
                await _apiClient.PostAsync<StockAjustRequestDto, StockOperationResultDto>(item.Endpoint, Deserialize<StockAjustRequestDto>(item), cancellationToken);
            }
            else if (item.Method == "Reserva")
            {
                await _apiClient.PostAsync<StockReservaRequestDto, StockOperationResultDto>(item.Endpoint, Deserialize<StockReservaRequestDto>(item), cancellationToken);
            }
            else if (item.Method == "Alliberament")
            {
                await _apiClient.PostAsync<StockAlliberamentRequestDto, StockOperationResultDto>(item.Endpoint, Deserialize<StockAlliberamentRequestDto>(item), cancellationToken);
            }
        }

        private static StockRequestDto ToRequest(StockRecord record) => new()
        {
            IdProducte = record.IdProducte,
            IdUbicacio = record.IdUbicacio,
            IdLot = record.IdLot,
            TotalsEnStock = record.TotalsEnStock,
            ReservatsPerComandes = record.ReservatsPerComandes
        };

        private static StockRecord ToRecord(StockReadDto dto, WarehouseSyncState state, string message) => new()
        {
            Id = dto.Id,
            IdProducte = dto.IdProducte,
            IdUbicacio = dto.IdUbicacio,
            IdLot = dto.IdLot,
            TotalsEnStock = dto.TotalsEnStock,
            ReservatsPerComandes = dto.ReservatsPerComandes,
            Disponibles = dto.Disponibles,
            SyncState = state,
            SyncMessage = message
        };

        private static StockRecord FromRequest(int id, StockRequestDto request, WarehouseSyncState state, string message) => new()
        {
            Id = id,
            IdProducte = request.IdProducte,
            IdUbicacio = request.IdUbicacio,
            IdLot = request.IdLot,
            TotalsEnStock = request.TotalsEnStock,
            ReservatsPerComandes = request.ReservatsPerComandes,
            Disponibles = Math.Max(0, request.TotalsEnStock - request.ReservatsPerComandes),
            SyncState = state,
            SyncMessage = message
        };

        private static StockRecord Clone(StockRecord record) => new()
        {
            Id = record.Id,
            IdProducte = record.IdProducte,
            IdUbicacio = record.IdUbicacio,
            IdLot = record.IdLot,
            TotalsEnStock = record.TotalsEnStock,
            ReservatsPerComandes = record.ReservatsPerComandes,
            Disponibles = record.Disponibles,
            ProducteText = record.ProducteText,
            UbicacioText = record.UbicacioText,
            LotText = record.LotText,
            SyncState = record.SyncState,
            SyncMessage = record.SyncMessage
        };

        private static StockMovementRecord ToMovementRecord(StockMovimentReadDto dto) => new()
        {
            Id = dto.Id,
            Tipus = dto.Tipus,
            IdProducte = dto.IdProducte,
            IdLot = dto.IdLot,
            IdUbicacioOrigen = dto.IdUbicacioOrigen,
            IdUbicacioDesti = dto.IdUbicacioDesti,
            Quantitat = dto.Quantitat,
            Motiu = dto.Motiu,
            DataMoviment = dto.DataMoviment
        };

        private static T Deserialize<T>(SyncQueueItem item)
        {
            return JsonSerializer.Deserialize<T>(item.Payload, StockJsonOptions)
                ?? throw new InvalidOperationException("Payload de stock invalid.");
        }

        private static StockRecord? FindByKey(IEnumerable<StockRecord> records, int idProducte, int idUbicacio, int? idLot)
        {
            return records.FirstOrDefault(record => record.IdProducte == idProducte && record.IdUbicacio == idUbicacio && record.IdLot == idLot);
        }

        private static int Available(StockRecord record)
        {
            return record.TotalsEnStock - record.ReservatsPerComandes;
        }

        private static int NextTemporaryStockId(IReadOnlyList<StockRecord> records)
        {
            var min = records.Count == 0 ? 0 : records.Min(record => record.Id);
            return min < 0 ? min - 1 : -1;
        }

        private static void ValidatePositive(int quantitat)
        {
            if (quantitat <= 0)
            {
                throw new InvalidOperationException("La quantitat ha de ser superior a 0.");
            }
        }
    }

    public sealed class LocationDataService : WarehouseEntityDataService<LocationRecord, UbicacionsReadDto, UbicacionsRequestDto>
    {
        public LocationDataService(ApiClient apiClient, LocalDatabase localDatabase, SyncQueue syncQueue)
            : base(apiClient, localDatabase, syncQueue, "ubicacions", "Ubicacions", "api/Ubicacions", "warehouse/locations/list/v1", ToRequest, ToRecord, FromRequest, Clone)
        {
        }

        public Task<WarehouseMutationResult<LocationRecord>> CreateManyAsync(
            IReadOnlyList<LocationRecord> drafts,
            IReadOnlyList<LocationRecord> current,
            CancellationToken cancellationToken = default)
        {
            return CreateManyAsync(drafts, current, "api/Ubicacions/bulk", cancellationToken);
        }

        private static UbicacionsRequestDto ToRequest(LocationRecord record) => new()
        {
            Zona = record.Zona,
            Passadis = record.Passadis,
            BlocEstanteria = record.BlocEstanteria,
            Fila = record.Fila,
            Columna = record.Columna
        };

        private static LocationRecord ToRecord(UbicacionsReadDto dto, WarehouseSyncState state, string message) => new()
        {
            Id = dto.Id,
            CodiGenerat = dto.CodiGenerat,
            Zona = dto.Zona,
            Passadis = dto.Passadis,
            BlocEstanteria = dto.BlocEstanteria,
            Fila = dto.Fila,
            Columna = dto.Columna,
            SyncState = state,
            SyncMessage = message
        };

        private static LocationRecord FromRequest(int id, UbicacionsRequestDto request, WarehouseSyncState state, string message) => new()
        {
            Id = id,
            Zona = request.Zona,
            Passadis = request.Passadis,
            BlocEstanteria = request.BlocEstanteria,
            Fila = request.Fila,
            Columna = request.Columna,
            SyncState = state,
            SyncMessage = message
        };

        private static LocationRecord Clone(LocationRecord record) => new()
        {
            Id = record.Id,
            CodiGenerat = record.CodiGenerat,
            Zona = record.Zona,
            Passadis = record.Passadis,
            BlocEstanteria = record.BlocEstanteria,
            Fila = record.Fila,
            Columna = record.Columna,
            SyncState = record.SyncState,
            SyncMessage = record.SyncMessage
        };
    }

    public sealed class SupplierLotDataService : WarehouseEntityDataService<SupplierLotRecord, ProveidorsLotReadDto, ProveidorsLotRequestDto>
    {
        public SupplierLotDataService(ApiClient apiClient, LocalDatabase localDatabase, SyncQueue syncQueue)
            : base(apiClient, localDatabase, syncQueue, "proveidors_lot", "Lots", "api/ProveidorsLots", "warehouse/lots/list/v1", ToRequest, ToRecord, FromRequest, Clone)
        {
        }

        private static ProveidorsLotRequestDto ToRequest(SupplierLotRecord record) => new()
        {
            IdProveidor = record.IdProveidor,
            IdProducte = record.IdProducte,
            QuantitatRebuda = record.QuantitatRebuda,
            DataDemanat = record.DataDemanat,
            DataRebut = record.DataRebut,
            DataCaducitat = record.DataCaducitat
        };

        private static SupplierLotRecord ToRecord(ProveidorsLotReadDto dto, WarehouseSyncState state, string message) => new()
        {
            Id = dto.Id,
            IdProveidor = dto.IdProveidor,
            IdProducte = dto.IdProducte,
            QuantitatRebuda = dto.QuantitatRebuda,
            DataDemanat = dto.DataDemanat,
            DataRebut = dto.DataRebut,
            DataCaducitat = dto.DataCaducitat,
            SyncState = state,
            SyncMessage = message
        };

        private static SupplierLotRecord FromRequest(int id, ProveidorsLotRequestDto request, WarehouseSyncState state, string message) => new()
        {
            Id = id,
            IdProveidor = request.IdProveidor,
            IdProducte = request.IdProducte,
            QuantitatRebuda = request.QuantitatRebuda,
            DataDemanat = request.DataDemanat,
            DataRebut = request.DataRebut,
            DataCaducitat = request.DataCaducitat,
            SyncState = state,
            SyncMessage = message
        };

        private static SupplierLotRecord Clone(SupplierLotRecord record) => new()
        {
            Id = record.Id,
            IdProveidor = record.IdProveidor,
            IdProducte = record.IdProducte,
            QuantitatRebuda = record.QuantitatRebuda,
            DataDemanat = record.DataDemanat,
            DataRebut = record.DataRebut,
            DataCaducitat = record.DataCaducitat,
            ProveidorText = record.ProveidorText,
            ProducteText = record.ProducteText,
            SyncState = record.SyncState,
            SyncMessage = record.SyncMessage
        };
    }
}
