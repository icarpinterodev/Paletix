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
    public sealed class ProductDataService
    {
        private const string EntityName = "products";
        private const string Endpoint = "api/Productes";
        private const string CacheKey = "products/list/v1";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly EntityApiService<ProductesReadDto, ProductesRequestDto> _api;
        private readonly LocalDatabase _localDatabase;
        private readonly SyncQueue _syncQueue;

        public ProductDataService(
            ApiClient apiClient,
            LocalDatabase localDatabase,
            SyncQueue syncQueue)
        {
            _api = new EntityApiService<ProductesReadDto, ProductesRequestDto>(apiClient, Endpoint);
            _localDatabase = localDatabase;
            _syncQueue = syncQueue;
        }

        public async Task<ProductLoadResult> LoadAsync(CancellationToken cancellationToken = default)
        {
            await _localDatabase.InitializeAsync(cancellationToken);

            if (!await CheckConnectionAsync(cancellationToken))
            {
                return await LoadCachedAsync("Productes carregats des de SQLite. API no disponible.", cancellationToken);
            }

            var syncResult = await SynchronizePendingAsync(cancellationToken);

            try
            {
                var products = await LoadFromApiAsync(cancellationToken);
                var cached = await ReadCacheAsync(cancellationToken);
                var active = await _syncQueue.GetActiveAsync(EntityName, cancellationToken);
                ApplyActiveOutbox(products, cached, active);
                await SaveCacheAsync(products, cancellationToken);

                var pending = await _syncQueue.GetPendingCountAsync(cancellationToken);
                var message = syncResult.ErrorCount > 0
                    ? $"API carregada amb {syncResult.ErrorCount} error(s) de sincronitzacio."
                    : pending > 0
                        ? $"API carregada. {pending} canvi(s) pendents."
                        : "Productes carregats des de l'API.";

                return new ProductLoadResult(products, true, pending, message);
            }
            catch (Exception ex)
            {
                var products = await ReadCacheAsync(cancellationToken) ?? new List<ProductRecord>();
                var active = await _syncQueue.GetActiveAsync(EntityName, cancellationToken);
                ApplyActiveOutbox(products, products, active);
                await SaveCacheAsync(products, cancellationToken);

                var pending = await _syncQueue.GetPendingCountAsync(cancellationToken);
                var message = products.Count == 0
                    ? $"No s'ha pogut carregar l'API ni SQLite: {ex.Message}"
                    : $"Productes carregats des de SQLite. {pending} canvi(s) pendents.";

                return new ProductLoadResult(products, false, pending, message);
            }
        }

        public async Task<ProductLoadResult> LoadCachedAsync(CancellationToken cancellationToken = default)
        {
            return await LoadCachedAsync("Productes carregats des de SQLite.", cancellationToken);
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

        public async Task<ProductMutationResult> CreateAsync(
            ProductRecord draft,
            IReadOnlyList<ProductRecord> current,
            CancellationToken cancellationToken = default)
        {
            await _localDatabase.InitializeAsync(cancellationToken);
            var request = ToRequest(draft);
            var products = current.Select(Clone).ToList();

            try
            {
                var created = await _api.CreateAsync(request, cancellationToken);
                var record = ToRecord(created, ProductSyncState.Synced, "Sincronitzat");
                products.Insert(0, record);
                await SaveCacheAsync(products, cancellationToken);
                return await ResultAsync(record, products, "Producte creat a l'API.", cancellationToken);
            }
            catch (Exception ex) when (IsConnectivityFailure(ex))
            {
                var record = FromRequest(NextTemporaryId(products), request, ProductSyncState.Pending, "Creacio pendent de sincronitzar");
                products.Insert(0, record);
                await _syncQueue.UpsertAsync(EntityName, record.IdText, "POST", Endpoint, request, "Pending", cancellationToken);
                await SaveCacheAsync(products, cancellationToken);
                return await ResultAsync(record, products, "Producte creat offline. Queda pendent de sincronitzar.", cancellationToken);
            }
            catch (Exception ex)
            {
                var record = FromRequest(NextTemporaryId(products), request, ProductSyncState.Error, ex.Message);
                products.Insert(0, record);
                await _syncQueue.UpsertAsync(EntityName, record.IdText, "POST", Endpoint, request, "Error", cancellationToken);
                await SaveCacheAsync(products, cancellationToken);
                return await ResultAsync(record, products, "L'API ha retornat error. El producte queda pendent en error sync.", cancellationToken);
            }
        }

        public async Task<ProductMutationResult> UpdateAsync(
            ProductRecord product,
            IReadOnlyList<ProductRecord> current,
            CancellationToken cancellationToken = default)
        {
            await _localDatabase.InitializeAsync(cancellationToken);
            var request = ToRequest(product);
            var products = current.Select(Clone).ToList();
            var index = products.FindIndex(item => item.Id == product.Id);

            if (product.Id < 0)
            {
                product.SyncState = ProductSyncState.Pending;
                product.SyncMessage = "Creacio pendent actualitzada";
                ReplaceOrAdd(products, product);
                await _syncQueue.UpsertAsync(EntityName, product.IdText, "POST", Endpoint, request, "Pending", cancellationToken);
                await SaveCacheAsync(products, cancellationToken);
                return await ResultAsync(product, products, "Producte pendent actualitzat offline.", cancellationToken);
            }

            try
            {
                await _api.UpdateAsync(product.Id, request, cancellationToken);
                product.SyncState = ProductSyncState.Synced;
                product.SyncMessage = "Sincronitzat";
                if (index >= 0)
                {
                    products[index] = Clone(product);
                }

                await _syncQueue.RemoveForEntityAsync(EntityName, product.IdText, cancellationToken);
                await SaveCacheAsync(products, cancellationToken);
                return await ResultAsync(product, products, "Producte actualitzat a l'API.", cancellationToken);
            }
            catch (Exception ex) when (IsConnectivityFailure(ex))
            {
                product.SyncState = ProductSyncState.Pending;
                product.SyncMessage = "Edicio pendent de sincronitzar";
                ReplaceOrAdd(products, product);
                await _syncQueue.UpsertAsync(EntityName, product.IdText, "PUT", $"{Endpoint}/{product.Id}", request, "Pending", cancellationToken);
                await SaveCacheAsync(products, cancellationToken);
                return await ResultAsync(product, products, "Producte actualitzat offline. Queda pendent de sincronitzar.", cancellationToken);
            }
            catch (Exception ex)
            {
                product.SyncState = ProductSyncState.Error;
                product.SyncMessage = ex.Message;
                ReplaceOrAdd(products, product);
                await _syncQueue.UpsertAsync(EntityName, product.IdText, "PUT", $"{Endpoint}/{product.Id}", request, "Error", cancellationToken);
                await SaveCacheAsync(products, cancellationToken);
                return await ResultAsync(product, products, "L'API ha retornat error. L'edicio queda en error sync.", cancellationToken);
            }
        }

        public async Task<ProductMutationResult> DeleteAsync(
            ProductRecord product,
            IReadOnlyList<ProductRecord> current,
            CancellationToken cancellationToken = default)
        {
            await _localDatabase.InitializeAsync(cancellationToken);
            var products = current.Select(Clone).ToList();

            if (product.Id < 0)
            {
                products.RemoveAll(item => item.Id == product.Id);
                await _syncQueue.RemoveForEntityAsync(EntityName, product.IdText, cancellationToken);
                await SaveCacheAsync(products, cancellationToken);
                return await ResultAsync(null, products, "Producte pendent descartat.", cancellationToken);
            }

            try
            {
                await _api.DeleteAsync(product.Id, cancellationToken);
                products.RemoveAll(item => item.Id == product.Id);
                await _syncQueue.RemoveForEntityAsync(EntityName, product.IdText, cancellationToken);
                await SaveCacheAsync(products, cancellationToken);
                return await ResultAsync(null, products, "Producte eliminat a l'API.", cancellationToken);
            }
            catch (Exception ex) when (IsConnectivityFailure(ex))
            {
                product.SyncState = ProductSyncState.PendingDelete;
                product.SyncMessage = "Eliminacio pendent de sincronitzar";
                ReplaceOrAdd(products, product);
                await _syncQueue.UpsertAsync(EntityName, product.IdText, "DELETE", $"{Endpoint}/{product.Id}", new { product.Id }, "Pending", cancellationToken);
                await SaveCacheAsync(products, cancellationToken);
                return await ResultAsync(product, products, "Producte marcat per eliminar offline.", cancellationToken);
            }
            catch (Exception ex)
            {
                product.SyncState = ProductSyncState.Error;
                product.SyncMessage = ex.Message;
                ReplaceOrAdd(products, product);
                await _syncQueue.UpsertAsync(EntityName, product.IdText, "DELETE", $"{Endpoint}/{product.Id}", new { product.Id }, "Error", cancellationToken);
                await SaveCacheAsync(products, cancellationToken);
                return await ResultAsync(product, products, "L'API ha retornat error. L'eliminacio queda en error sync.", cancellationToken);
            }
        }

        private async Task<SyncProcessResult> SynchronizePendingAsync(CancellationToken cancellationToken)
        {
            var pending = await _syncQueue.GetPendingAsync(EntityName, cancellationToken);
            if (pending.Count == 0)
            {
                return new SyncProcessResult(0, 0);
            }

            var cached = await ReadCacheAsync(cancellationToken) ?? new List<ProductRecord>();
            var synced = 0;
            var errors = 0;

            foreach (var item in pending)
            {
                try
                {
                    if (item.Method == "POST")
                    {
                        var request = DeserializePayload<ProductesRequestDto>(item);
                        var created = await _api.CreateAsync(request, cancellationToken);
                        cached.RemoveAll(product => product.IdText == item.EntityId);
                        cached.Insert(0, ToRecord(created, ProductSyncState.Synced, "Sincronitzat"));
                    }
                    else if (item.Method == "PUT" && int.TryParse(item.EntityId, out var updateId))
                    {
                        var request = DeserializePayload<ProductesRequestDto>(item);
                        await _api.UpdateAsync(updateId, request, cancellationToken);
                        var index = cached.FindIndex(product => product.Id == updateId);
                        if (index >= 0)
                        {
                            cached[index] = FromRequest(updateId, request, ProductSyncState.Synced, "Sincronitzat");
                        }
                    }
                    else if (item.Method == "DELETE" && int.TryParse(item.EntityId, out var deleteId))
                    {
                        await _api.DeleteAsync(deleteId, cancellationToken);
                        cached.RemoveAll(product => product.Id == deleteId);
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

        private async Task<List<ProductRecord>> LoadFromApiAsync(CancellationToken cancellationToken)
        {
            var first = await _api.GetPageAsync(1, 100, cancellationToken);
            var products = first.Items
                .Select(item => ToRecord(item, ProductSyncState.Synced, "Sincronitzat"))
                .ToList();

            for (var page = 2; page <= first.TotalPages; page++)
            {
                var next = await _api.GetPageAsync(page, 100, cancellationToken);
                products.AddRange(next.Items.Select(item => ToRecord(item, ProductSyncState.Synced, "Sincronitzat")));
            }

            return products;
        }

        private async Task<ProductLoadResult> LoadCachedAsync(string message, CancellationToken cancellationToken)
        {
            await _localDatabase.InitializeAsync(cancellationToken);
            var products = await ReadCacheAsync(cancellationToken) ?? new List<ProductRecord>();
            var active = await _syncQueue.GetActiveAsync(EntityName, cancellationToken);
            ApplyActiveOutbox(products, products, active);
            await SaveCacheAsync(products, cancellationToken);

            var pending = await _syncQueue.GetPendingCountAsync(cancellationToken);
            var finalMessage = products.Count == 0
                ? "SQLite encara no te dades locals de productes."
                : pending > 0
                    ? $"{message} {pending} canvi(s) pendents."
                    : message;

            return new ProductLoadResult(products, false, pending, finalMessage);
        }

        private static void ApplyActiveOutbox(
            List<ProductRecord> products,
            IReadOnlyList<ProductRecord>? cached,
            IReadOnlyList<SyncQueueItem> active)
        {
            foreach (var item in active)
            {
                var state = item.Status == "Error" ? ProductSyncState.Error : ProductSyncState.Pending;
                if (item.Method == "POST")
                {
                    var request = DeserializePayload<ProductesRequestDto>(item);
                    var id = int.TryParse(item.EntityId, out var tempId) ? tempId : -1;
                    ReplaceOrAdd(products, FromRequest(id, request, state, item.Status == "Error" ? "Error sync" : "Creacio pendent"));
                }
                else if (item.Method == "PUT" && int.TryParse(item.EntityId, out var updateId))
                {
                    var request = DeserializePayload<ProductesRequestDto>(item);
                    ReplaceOrAdd(products, FromRequest(updateId, request, state, item.Status == "Error" ? "Error sync" : "Edicio pendent"));
                }
                else if (item.Method == "DELETE" && int.TryParse(item.EntityId, out var deleteId))
                {
                    var existing = products.FirstOrDefault(product => product.Id == deleteId)
                        ?? cached?.FirstOrDefault(product => product.Id == deleteId);
                    if (existing is not null)
                    {
                        existing.SyncState = item.Status == "Error" ? ProductSyncState.Error : ProductSyncState.PendingDelete;
                        existing.SyncMessage = item.Status == "Error" ? "Error sync" : "Eliminacio pendent";
                        ReplaceOrAdd(products, existing);
                    }
                }
            }
        }

        private async Task<List<ProductRecord>?> ReadCacheAsync(CancellationToken cancellationToken)
        {
            return await _localDatabase.GetJsonAsync<List<ProductRecord>>(CacheKey, cancellationToken);
        }

        private async Task SaveCacheAsync(IReadOnlyList<ProductRecord> products, CancellationToken cancellationToken)
        {
            var sanitized = products.Select(Clone).ToList();
            foreach (var product in sanitized)
            {
                product.IsSelected = false;
            }

            await _localDatabase.SetJsonAsync(CacheKey, sanitized, cancellationToken);
        }

        private async Task<ProductMutationResult> ResultAsync(
            ProductRecord? product,
            IReadOnlyList<ProductRecord> products,
            string message,
            CancellationToken cancellationToken)
        {
            var pending = await _syncQueue.GetPendingCountAsync(cancellationToken);
            return new ProductMutationResult(product, products, pending, message);
        }

        private static ProductesRequestDto ToRequest(ProductRecord product)
        {
            return new ProductesRequestDto
            {
                Referencia = EmptyToNull(product.Referencia),
                Nom = string.IsNullOrWhiteSpace(product.Nom) ? "Producte sense nom" : product.Nom.Trim(),
                Descripcio = EmptyToNull(product.Descripcio),
                IdTipus = product.IdTipus,
                VolumMl = product.VolumMl,
                IdProveidor = product.IdProveidor,
                IdUbicacio = product.IdUbicacio,
                CaixesPerPalet = product.CaixesPerPalet,
                ImatgeUrl = EmptyToNull(product.ImatgeUrl),
                Actiu = product.Actiu,
                PreuVendaCaixa = product.PreuVendaCaixa,
                CostPerCaixa = product.CostPerCaixa,
                EstabilitatAlPalet = product.EstabilitatAlPalet,
                PesKg = product.PesKg
            };
        }

        private static ProductRecord ToRecord(ProductesReadDto dto, ProductSyncState state, string message)
        {
            return new ProductRecord
            {
                Id = dto.Id,
                Referencia = dto.Referencia,
                Nom = dto.Nom,
                Descripcio = dto.Descripcio,
                IdTipus = dto.IdTipus,
                VolumMl = dto.VolumMl,
                IdProveidor = dto.IdProveidor,
                IdUbicacio = dto.IdUbicacio,
                CaixesPerPalet = dto.CaixesPerPalet,
                ImatgeUrl = dto.ImatgeUrl,
                Actiu = dto.Actiu,
                PreuVendaCaixa = dto.PreuVendaCaixa,
                CostPerCaixa = dto.CostPerCaixa,
                EstabilitatAlPalet = dto.EstabilitatAlPalet,
                PesKg = dto.PesKg,
                DataAfegit = dto.DataAfegit,
                SyncState = state,
                SyncMessage = message
            };
        }

        private static ProductRecord FromRequest(int id, ProductesRequestDto request, ProductSyncState state, string message)
        {
            return new ProductRecord
            {
                Id = id,
                Referencia = request.Referencia,
                Nom = request.Nom,
                Descripcio = request.Descripcio,
                IdTipus = request.IdTipus,
                VolumMl = request.VolumMl,
                IdProveidor = request.IdProveidor,
                IdUbicacio = request.IdUbicacio,
                CaixesPerPalet = request.CaixesPerPalet,
                ImatgeUrl = request.ImatgeUrl,
                Actiu = request.Actiu,
                PreuVendaCaixa = request.PreuVendaCaixa,
                CostPerCaixa = request.CostPerCaixa,
                EstabilitatAlPalet = request.EstabilitatAlPalet,
                PesKg = request.PesKg,
                SyncState = state,
                SyncMessage = message
            };
        }

        private static ProductRecord Clone(ProductRecord product)
        {
            return new ProductRecord
            {
                Id = product.Id,
                Referencia = product.Referencia,
                Nom = product.Nom,
                Descripcio = product.Descripcio,
                IdTipus = product.IdTipus,
                VolumMl = product.VolumMl,
                IdProveidor = product.IdProveidor,
                IdUbicacio = product.IdUbicacio,
                CaixesPerPalet = product.CaixesPerPalet,
                ImatgeUrl = product.ImatgeUrl,
                Actiu = product.Actiu,
                PreuVendaCaixa = product.PreuVendaCaixa,
                CostPerCaixa = product.CostPerCaixa,
                EstabilitatAlPalet = product.EstabilitatAlPalet,
                PesKg = product.PesKg,
                DataAfegit = product.DataAfegit,
                SyncState = product.SyncState,
                SyncMessage = product.SyncMessage
            };
        }

        private static T DeserializePayload<T>(SyncQueueItem item)
        {
            return JsonSerializer.Deserialize<T>(item.Payload, JsonOptions)
                ?? throw new InvalidOperationException("Payload de sincronitzacio invalid.");
        }

        private static void ReplaceOrAdd(List<ProductRecord> products, ProductRecord product)
        {
            var index = products.FindIndex(item => item.Id == product.Id);
            if (index >= 0)
            {
                products[index] = Clone(product);
                return;
            }

            products.Insert(0, Clone(product));
        }

        private static void MarkCachedError(List<ProductRecord> cached, SyncQueueItem item)
        {
            var existing = cached.FirstOrDefault(product => product.IdText == item.EntityId);
            if (existing is null)
            {
                return;
            }

            existing.SyncState = ProductSyncState.Error;
            existing.SyncMessage = "Error sync";
        }

        private static int NextTemporaryId(IReadOnlyList<ProductRecord> products)
        {
            var min = products.Count == 0 ? 0 : products.Min(product => product.Id);
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

    public sealed record ProductLoadResult(
        IReadOnlyList<ProductRecord> Products,
        bool IsOnline,
        int PendingCount,
        string Message);

    public sealed record ProductMutationResult(
        ProductRecord? Product,
        IReadOnlyList<ProductRecord> Products,
        int PendingCount,
        string Message);
}
