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
    public sealed class SupplierDataService
    {
        private const string EntityName = "suppliers";
        private const string Endpoint = "api/Proveidors";
        private const string CacheKey = "suppliers/list/v1";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly EntityApiService<ProveidorsReadDto, ProveidorsRequestDto> _api;
        private readonly LocalDatabase _localDatabase;
        private readonly SyncQueue _syncQueue;

        public SupplierDataService(
            ApiClient apiClient,
            LocalDatabase localDatabase,
            SyncQueue syncQueue)
        {
            _api = new EntityApiService<ProveidorsReadDto, ProveidorsRequestDto>(apiClient, Endpoint);
            _localDatabase = localDatabase;
            _syncQueue = syncQueue;
        }

        public async Task<SupplierLoadResult> LoadAsync(CancellationToken cancellationToken = default)
        {
            await _localDatabase.InitializeAsync(cancellationToken);

            if (!await CheckConnectionAsync(cancellationToken))
            {
                return await LoadCachedAsync("Proveidors carregats des de SQLite. API no disponible.", cancellationToken);
            }

            var syncResult = await SynchronizePendingAsync(cancellationToken);

            try
            {
                var suppliers = await LoadFromApiAsync(cancellationToken);
                var cached = await ReadCacheAsync(cancellationToken);
                var active = await _syncQueue.GetActiveAsync(EntityName, cancellationToken);
                ApplyActiveOutbox(suppliers, cached, active);
                await SaveCacheAsync(suppliers, cancellationToken);

                var pending = await _syncQueue.GetPendingCountAsync(cancellationToken);
                var message = syncResult.ErrorCount > 0
                    ? $"API carregada amb {syncResult.ErrorCount} error(s) de sincronitzacio."
                    : pending > 0
                        ? $"API carregada. {pending} canvi(s) pendents."
                        : "Proveidors carregats des de l'API.";

                return new SupplierLoadResult(suppliers, true, pending, message);
            }
            catch (Exception ex)
            {
                var suppliers = await ReadCacheAsync(cancellationToken) ?? new List<SupplierRecord>();
                var active = await _syncQueue.GetActiveAsync(EntityName, cancellationToken);
                ApplyActiveOutbox(suppliers, suppliers, active);
                await SaveCacheAsync(suppliers, cancellationToken);

                var pending = await _syncQueue.GetPendingCountAsync(cancellationToken);
                var message = suppliers.Count == 0
                    ? $"No s'ha pogut carregar l'API ni SQLite: {ex.Message}"
                    : $"Proveidors carregats des de SQLite. {pending} canvi(s) pendents.";

                return new SupplierLoadResult(suppliers, false, pending, message);
            }
        }

        public async Task<SupplierLoadResult> LoadCachedAsync(CancellationToken cancellationToken = default)
        {
            return await LoadCachedAsync("Proveidors carregats des de SQLite.", cancellationToken);
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

        public async Task<SupplierMutationResult> CreateAsync(
            SupplierRecord draft,
            IReadOnlyList<SupplierRecord> current,
            CancellationToken cancellationToken = default)
        {
            await _localDatabase.InitializeAsync(cancellationToken);
            var request = ToRequest(draft);
            var suppliers = current.Select(Clone).ToList();

            try
            {
                var created = await _api.CreateAsync(request, cancellationToken);
                var record = ToRecord(created, SupplierSyncState.Synced, "Sincronitzat");
                suppliers.Insert(0, record);
                await SaveCacheAsync(suppliers, cancellationToken);
                return await ResultAsync(record, suppliers, "Proveidor creat a l'API.", cancellationToken);
            }
            catch (Exception ex) when (IsConnectivityFailure(ex))
            {
                var record = FromRequest(NextTemporaryId(suppliers), request, SupplierSyncState.Pending, "Creacio pendent de sincronitzar");
                suppliers.Insert(0, record);
                await _syncQueue.UpsertAsync(EntityName, record.IdText, "POST", Endpoint, request, "Pending", cancellationToken);
                await SaveCacheAsync(suppliers, cancellationToken);
                return await ResultAsync(record, suppliers, "Proveidor creat offline. Queda pendent de sincronitzar.", cancellationToken);
            }
            catch (Exception ex)
            {
                var record = FromRequest(NextTemporaryId(suppliers), request, SupplierSyncState.Error, ex.Message);
                suppliers.Insert(0, record);
                await _syncQueue.UpsertAsync(EntityName, record.IdText, "POST", Endpoint, request, "Error", cancellationToken);
                await SaveCacheAsync(suppliers, cancellationToken);
                return await ResultAsync(record, suppliers, "L'API ha retornat error. El proveidor queda en error sync.", cancellationToken);
            }
        }

        public async Task<SupplierMutationResult> UpdateAsync(
            SupplierRecord supplier,
            IReadOnlyList<SupplierRecord> current,
            CancellationToken cancellationToken = default)
        {
            await _localDatabase.InitializeAsync(cancellationToken);
            var request = ToRequest(supplier);
            var suppliers = current.Select(Clone).ToList();
            var index = suppliers.FindIndex(item => item.Id == supplier.Id);

            if (supplier.Id < 0)
            {
                supplier.SyncState = SupplierSyncState.Pending;
                supplier.SyncMessage = "Creacio pendent actualitzada";
                ReplaceOrAdd(suppliers, supplier);
                await _syncQueue.UpsertAsync(EntityName, supplier.IdText, "POST", Endpoint, request, "Pending", cancellationToken);
                await SaveCacheAsync(suppliers, cancellationToken);
                return await ResultAsync(supplier, suppliers, "Proveidor pendent actualitzat offline.", cancellationToken);
            }

            try
            {
                await _api.UpdateAsync(supplier.Id, request, cancellationToken);
                supplier.SyncState = SupplierSyncState.Synced;
                supplier.SyncMessage = "Sincronitzat";
                if (index >= 0)
                {
                    suppliers[index] = Clone(supplier);
                }

                await _syncQueue.RemoveForEntityAsync(EntityName, supplier.IdText, cancellationToken);
                await SaveCacheAsync(suppliers, cancellationToken);
                return await ResultAsync(supplier, suppliers, "Proveidor actualitzat a l'API.", cancellationToken);
            }
            catch (Exception ex) when (IsConnectivityFailure(ex))
            {
                supplier.SyncState = SupplierSyncState.Pending;
                supplier.SyncMessage = "Edicio pendent de sincronitzar";
                ReplaceOrAdd(suppliers, supplier);
                await _syncQueue.UpsertAsync(EntityName, supplier.IdText, "PUT", $"{Endpoint}/{supplier.Id}", request, "Pending", cancellationToken);
                await SaveCacheAsync(suppliers, cancellationToken);
                return await ResultAsync(supplier, suppliers, "Proveidor actualitzat offline. Queda pendent de sincronitzar.", cancellationToken);
            }
            catch (Exception ex)
            {
                supplier.SyncState = SupplierSyncState.Error;
                supplier.SyncMessage = ex.Message;
                ReplaceOrAdd(suppliers, supplier);
                await _syncQueue.UpsertAsync(EntityName, supplier.IdText, "PUT", $"{Endpoint}/{supplier.Id}", request, "Error", cancellationToken);
                await SaveCacheAsync(suppliers, cancellationToken);
                return await ResultAsync(supplier, suppliers, "L'API ha retornat error. L'edicio queda en error sync.", cancellationToken);
            }
        }

        public async Task<SupplierMutationResult> DeleteAsync(
            SupplierRecord supplier,
            IReadOnlyList<SupplierRecord> current,
            CancellationToken cancellationToken = default)
        {
            await _localDatabase.InitializeAsync(cancellationToken);
            var suppliers = current.Select(Clone).ToList();

            if (supplier.Id < 0)
            {
                suppliers.RemoveAll(item => item.Id == supplier.Id);
                await _syncQueue.RemoveForEntityAsync(EntityName, supplier.IdText, cancellationToken);
                await SaveCacheAsync(suppliers, cancellationToken);
                return await ResultAsync(null, suppliers, "Proveidor pendent descartat.", cancellationToken);
            }

            try
            {
                await _api.DeleteAsync(supplier.Id, cancellationToken);
                suppliers.RemoveAll(item => item.Id == supplier.Id);
                await _syncQueue.RemoveForEntityAsync(EntityName, supplier.IdText, cancellationToken);
                await SaveCacheAsync(suppliers, cancellationToken);
                return await ResultAsync(null, suppliers, "Proveidor eliminat a l'API.", cancellationToken);
            }
            catch (Exception ex) when (IsConnectivityFailure(ex))
            {
                supplier.SyncState = SupplierSyncState.PendingDelete;
                supplier.SyncMessage = "Eliminacio pendent de sincronitzar";
                ReplaceOrAdd(suppliers, supplier);
                await _syncQueue.UpsertAsync(EntityName, supplier.IdText, "DELETE", $"{Endpoint}/{supplier.Id}", new { supplier.Id }, "Pending", cancellationToken);
                await SaveCacheAsync(suppliers, cancellationToken);
                return await ResultAsync(supplier, suppliers, "Proveidor marcat per eliminar offline.", cancellationToken);
            }
            catch (Exception ex)
            {
                supplier.SyncState = SupplierSyncState.Error;
                supplier.SyncMessage = ex.Message;
                ReplaceOrAdd(suppliers, supplier);
                await _syncQueue.UpsertAsync(EntityName, supplier.IdText, "DELETE", $"{Endpoint}/{supplier.Id}", new { supplier.Id }, "Error", cancellationToken);
                await SaveCacheAsync(suppliers, cancellationToken);
                return await ResultAsync(supplier, suppliers, "L'API ha retornat error. L'eliminacio queda en error sync.", cancellationToken);
            }
        }

        private async Task<SyncProcessResult> SynchronizePendingAsync(CancellationToken cancellationToken)
        {
            var pending = await _syncQueue.GetPendingAsync(EntityName, cancellationToken);
            if (pending.Count == 0)
            {
                return new SyncProcessResult(0, 0);
            }

            var cached = await ReadCacheAsync(cancellationToken) ?? new List<SupplierRecord>();
            var synced = 0;
            var errors = 0;

            foreach (var item in pending)
            {
                try
                {
                    if (item.Method == "POST")
                    {
                        var request = DeserializePayload<ProveidorsRequestDto>(item);
                        var created = await _api.CreateAsync(request, cancellationToken);
                        cached.RemoveAll(supplier => supplier.IdText == item.EntityId);
                        cached.Insert(0, ToRecord(created, SupplierSyncState.Synced, "Sincronitzat"));
                    }
                    else if (item.Method == "PUT" && int.TryParse(item.EntityId, out var updateId))
                    {
                        var request = DeserializePayload<ProveidorsRequestDto>(item);
                        await _api.UpdateAsync(updateId, request, cancellationToken);
                        var index = cached.FindIndex(supplier => supplier.Id == updateId);
                        if (index >= 0)
                        {
                            cached[index] = FromRequest(updateId, request, SupplierSyncState.Synced, "Sincronitzat");
                        }
                    }
                    else if (item.Method == "DELETE" && int.TryParse(item.EntityId, out var deleteId))
                    {
                        await _api.DeleteAsync(deleteId, cancellationToken);
                        cached.RemoveAll(supplier => supplier.Id == deleteId);
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

        private async Task<List<SupplierRecord>> LoadFromApiAsync(CancellationToken cancellationToken)
        {
            var first = await _api.GetPageAsync(1, 100, cancellationToken);
            var suppliers = first.Items
                .Select(item => ToRecord(item, SupplierSyncState.Synced, "Sincronitzat"))
                .ToList();

            for (var page = 2; page <= first.TotalPages; page++)
            {
                var next = await _api.GetPageAsync(page, 100, cancellationToken);
                suppliers.AddRange(next.Items.Select(item => ToRecord(item, SupplierSyncState.Synced, "Sincronitzat")));
            }

            return suppliers;
        }

        private async Task<SupplierLoadResult> LoadCachedAsync(string message, CancellationToken cancellationToken)
        {
            await _localDatabase.InitializeAsync(cancellationToken);
            var suppliers = await ReadCacheAsync(cancellationToken) ?? new List<SupplierRecord>();
            var active = await _syncQueue.GetActiveAsync(EntityName, cancellationToken);
            ApplyActiveOutbox(suppliers, suppliers, active);
            await SaveCacheAsync(suppliers, cancellationToken);

            var pending = await _syncQueue.GetPendingCountAsync(cancellationToken);
            var finalMessage = suppliers.Count == 0
                ? "SQLite encara no te dades locals de proveidors."
                : pending > 0
                    ? $"{message} {pending} canvi(s) pendents."
                    : message;

            return new SupplierLoadResult(suppliers, false, pending, finalMessage);
        }

        private static void ApplyActiveOutbox(
            List<SupplierRecord> suppliers,
            IReadOnlyList<SupplierRecord>? cached,
            IReadOnlyList<SyncQueueItem> active)
        {
            foreach (var item in active)
            {
                var state = item.Status == "Error" ? SupplierSyncState.Error : SupplierSyncState.Pending;
                if (item.Method == "POST")
                {
                    var request = DeserializePayload<ProveidorsRequestDto>(item);
                    var id = int.TryParse(item.EntityId, out var tempId) ? tempId : -1;
                    ReplaceOrAdd(suppliers, FromRequest(id, request, state, item.Status == "Error" ? "Error sync" : "Creacio pendent"));
                }
                else if (item.Method == "PUT" && int.TryParse(item.EntityId, out var updateId))
                {
                    var request = DeserializePayload<ProveidorsRequestDto>(item);
                    ReplaceOrAdd(suppliers, FromRequest(updateId, request, state, item.Status == "Error" ? "Error sync" : "Edicio pendent"));
                }
                else if (item.Method == "DELETE" && int.TryParse(item.EntityId, out var deleteId))
                {
                    var existing = suppliers.FirstOrDefault(supplier => supplier.Id == deleteId)
                        ?? cached?.FirstOrDefault(supplier => supplier.Id == deleteId);
                    if (existing is not null)
                    {
                        existing.SyncState = item.Status == "Error" ? SupplierSyncState.Error : SupplierSyncState.PendingDelete;
                        existing.SyncMessage = item.Status == "Error" ? "Error sync" : "Eliminacio pendent";
                        ReplaceOrAdd(suppliers, existing);
                    }
                }
            }
        }

        private async Task<List<SupplierRecord>?> ReadCacheAsync(CancellationToken cancellationToken)
        {
            return await _localDatabase.GetJsonAsync<List<SupplierRecord>>(CacheKey, cancellationToken);
        }

        private async Task SaveCacheAsync(IReadOnlyList<SupplierRecord> suppliers, CancellationToken cancellationToken)
        {
            var sanitized = suppliers.Select(Clone).ToList();
            foreach (var supplier in sanitized)
            {
                supplier.IsSelected = false;
            }

            await _localDatabase.SetJsonAsync(CacheKey, sanitized, cancellationToken);
        }

        private async Task<SupplierMutationResult> ResultAsync(
            SupplierRecord? supplier,
            IReadOnlyList<SupplierRecord> suppliers,
            string message,
            CancellationToken cancellationToken)
        {
            var pending = await _syncQueue.GetPendingCountAsync(cancellationToken);
            return new SupplierMutationResult(supplier, suppliers, pending, message);
        }

        private static ProveidorsRequestDto ToRequest(SupplierRecord supplier)
        {
            return new ProveidorsRequestDto
            {
                MarcaMatriu = EmptyToNull(supplier.MarcaMatriu),
                NomEmpresa = string.IsNullOrWhiteSpace(supplier.NomEmpresa) ? "Proveidor sense nom" : supplier.NomEmpresa.Trim(),
                Telefon = string.IsNullOrWhiteSpace(supplier.Telefon) ? "-" : supplier.Telefon.Trim(),
                Email = string.IsNullOrWhiteSpace(supplier.Email) ? "-" : supplier.Email.Trim(),
                Adreca = EmptyToNull(supplier.Adreca),
                UrlWeb = EmptyToNull(supplier.UrlWeb),
                IdTipusProductePrincipal = supplier.IdTipusProductePrincipal
            };
        }

        private static SupplierRecord ToRecord(ProveidorsReadDto dto, SupplierSyncState state, string message)
        {
            return new SupplierRecord
            {
                Id = dto.Id,
                MarcaMatriu = dto.MarcaMatriu,
                NomEmpresa = dto.NomEmpresa,
                Telefon = dto.Telefon,
                Email = dto.Email,
                Adreca = dto.Adreca,
                UrlWeb = dto.UrlWeb,
                IdTipusProductePrincipal = dto.IdTipusProductePrincipal,
                SyncState = state,
                SyncMessage = message
            };
        }

        private static SupplierRecord FromRequest(int id, ProveidorsRequestDto request, SupplierSyncState state, string message)
        {
            return new SupplierRecord
            {
                Id = id,
                MarcaMatriu = request.MarcaMatriu,
                NomEmpresa = request.NomEmpresa,
                Telefon = request.Telefon,
                Email = request.Email,
                Adreca = request.Adreca,
                UrlWeb = request.UrlWeb,
                IdTipusProductePrincipal = request.IdTipusProductePrincipal,
                SyncState = state,
                SyncMessage = message
            };
        }

        private static SupplierRecord Clone(SupplierRecord supplier)
        {
            return new SupplierRecord
            {
                Id = supplier.Id,
                MarcaMatriu = supplier.MarcaMatriu,
                NomEmpresa = supplier.NomEmpresa,
                Telefon = supplier.Telefon,
                Email = supplier.Email,
                Adreca = supplier.Adreca,
                UrlWeb = supplier.UrlWeb,
                IdTipusProductePrincipal = supplier.IdTipusProductePrincipal,
                SyncState = supplier.SyncState,
                SyncMessage = supplier.SyncMessage
            };
        }

        private static T DeserializePayload<T>(SyncQueueItem item)
        {
            return JsonSerializer.Deserialize<T>(item.Payload, JsonOptions)
                ?? throw new InvalidOperationException("Payload de sincronitzacio invalid.");
        }

        private static void ReplaceOrAdd(List<SupplierRecord> suppliers, SupplierRecord supplier)
        {
            var index = suppliers.FindIndex(item => item.Id == supplier.Id);
            if (index >= 0)
            {
                suppliers[index] = Clone(supplier);
                return;
            }

            suppliers.Insert(0, Clone(supplier));
        }

        private static void MarkCachedError(List<SupplierRecord> cached, SyncQueueItem item)
        {
            var existing = cached.FirstOrDefault(supplier => supplier.IdText == item.EntityId);
            if (existing is null)
            {
                return;
            }

            existing.SyncState = SupplierSyncState.Error;
            existing.SyncMessage = "Error sync";
        }

        private static int NextTemporaryId(IReadOnlyList<SupplierRecord> suppliers)
        {
            var min = suppliers.Count == 0 ? 0 : suppliers.Min(supplier => supplier.Id);
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

        private sealed record SyncProcessResult(int SyncedCount, int ErrorCount);
    }

    public sealed record SupplierLoadResult(
        IReadOnlyList<SupplierRecord> Suppliers,
        bool IsOnline,
        int PendingCount,
        string Message);

    public sealed record SupplierMutationResult(
        SupplierRecord? Supplier,
        IReadOnlyList<SupplierRecord> Suppliers,
        int PendingCount,
        string Message);
}
