using PaletixDesktop.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PaletixDesktop.Services
{
    public sealed class PendingSyncService : ViewModels.ViewModelBase
    {
        private readonly SyncQueue _syncQueue;
        private readonly LocalDatabase _localDatabase;
        private readonly ClientDataService _clientDataService;
        private readonly SupplierDataService _supplierDataService;
        private readonly ProductDataService _productDataService;
        private readonly OrderDataService _orderDataService;
        private readonly StockDataService _stockDataService;
        private readonly LocationDataService _locationDataService;
        private readonly SupplierLotDataService _supplierLotDataService;
        private readonly PickingDataService _pickingDataService;
        private readonly AdminIdentityDataService _adminIdentityDataService;
        private string _statusText = "Sense canvis pendents.";

        public PendingSyncService(
            SyncQueue syncQueue,
            LocalDatabase localDatabase,
            ClientDataService clientDataService,
            SupplierDataService supplierDataService,
            ProductDataService productDataService,
            OrderDataService orderDataService,
            StockDataService stockDataService,
            LocationDataService locationDataService,
            SupplierLotDataService supplierLotDataService,
            PickingDataService pickingDataService,
            AdminIdentityDataService adminIdentityDataService)
        {
            _syncQueue = syncQueue;
            _localDatabase = localDatabase;
            _clientDataService = clientDataService;
            _supplierDataService = supplierDataService;
            _productDataService = productDataService;
            _orderDataService = orderDataService;
            _stockDataService = stockDataService;
            _locationDataService = locationDataService;
            _supplierLotDataService = supplierLotDataService;
            _pickingDataService = pickingDataService;
            _adminIdentityDataService = adminIdentityDataService;
        }

        public ObservableCollection<PendingSyncOperation> Items { get; } = new();

        public string StatusText
        {
            get => _statusText;
            private set => SetProperty(ref _statusText, value);
        }

        public bool HasItems => Items.Count > 0;
        public string CountText => Items.Count == 0 ? "" : Items.Count.ToString();

        public async Task RefreshAsync(CancellationToken cancellationToken = default)
        {
            IsBusy = true;
            try
            {
                var items = new List<PendingSyncOperation>();
                items.AddRange((await _syncQueue.GetActiveAsync("clients", cancellationToken)).Select(ToOperation));
                items.AddRange((await _syncQueue.GetActiveAsync("suppliers", cancellationToken)).Select(ToOperation));
                items.AddRange((await _syncQueue.GetActiveAsync("products", cancellationToken)).Select(ToOperation));
                items.AddRange((await _syncQueue.GetActiveAsync("orders", cancellationToken)).Select(ToOperation));
                items.AddRange((await _syncQueue.GetActiveAsync("stock", cancellationToken)).Select(ToOperation));
                items.AddRange((await _syncQueue.GetActiveAsync("stock_operations", cancellationToken)).Select(ToOperation));
                items.AddRange((await _syncQueue.GetActiveAsync("ubicacions", cancellationToken)).Select(ToOperation));
                items.AddRange((await _syncQueue.GetActiveAsync("proveidors_lot", cancellationToken)).Select(ToOperation));
                items.AddRange((await _syncQueue.GetActiveAsync("picking_lines", cancellationToken)).Select(ToOperation));
                items.AddRange((await _syncQueue.GetActiveAsync(AdminIdentityDataService.UsersEntityName, cancellationToken)).Select(ToOperation));
                items.AddRange((await _syncQueue.GetActiveAsync(AdminIdentityDataService.RolesEntityName, cancellationToken)).Select(ToOperation));
                items.AddRange((await _syncQueue.GetActiveAsync(AdminIdentityDataService.JobTitlesEntityName, cancellationToken)).Select(ToOperation));

                Items.Clear();
                foreach (var item in items.OrderByDescending(item => item.IsError).ThenBy(item => item.UpdatedUtc))
                {
                    Items.Add(item);
                }

                StatusText = Items.Count == 0
                    ? "No hi ha canvis pendents de sincronitzar."
                    : $"{Items.Count} canvi(s) pendents o en error.";
                RaiseCounts();
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task RetryAllAsync(CancellationToken cancellationToken = default)
        {
            IsBusy = true;
            try
            {
                await _syncQueue.MarkErrorsPendingAsync("clients", cancellationToken);
                await _syncQueue.MarkErrorsPendingAsync("suppliers", cancellationToken);
                await _syncQueue.MarkErrorsPendingAsync("products", cancellationToken);
                await _syncQueue.MarkErrorsPendingAsync("orders", cancellationToken);
                await _syncQueue.MarkErrorsPendingAsync("stock", cancellationToken);
                await _syncQueue.MarkErrorsPendingAsync("stock_operations", cancellationToken);
                await _syncQueue.MarkErrorsPendingAsync("ubicacions", cancellationToken);
                await _syncQueue.MarkErrorsPendingAsync("proveidors_lot", cancellationToken);
                await _syncQueue.MarkErrorsPendingAsync("picking_lines", cancellationToken);
                await _syncQueue.MarkErrorsPendingAsync(AdminIdentityDataService.UsersEntityName, cancellationToken);
                await _syncQueue.MarkErrorsPendingAsync(AdminIdentityDataService.RolesEntityName, cancellationToken);
                await _syncQueue.MarkErrorsPendingAsync(AdminIdentityDataService.JobTitlesEntityName, cancellationToken);
                await _clientDataService.LoadAsync(cancellationToken);
                await _supplierDataService.LoadAsync(cancellationToken);
                await _productDataService.LoadAsync(cancellationToken);
                await _orderDataService.LoadAsync(cancellationToken);
                await _stockDataService.LoadAsync(cancellationToken);
                await _locationDataService.LoadAsync(cancellationToken);
                await _supplierLotDataService.LoadAsync(cancellationToken);
                await _pickingDataService.LoadAsync(cancellationToken);
                await _adminIdentityDataService.LoadAsync(cancellationToken);
            }
            finally
            {
                IsBusy = false;
            }

            await RefreshAsync(cancellationToken);
        }

        public async Task RetryAsync(PendingSyncOperation operation, CancellationToken cancellationToken = default)
        {
            IsBusy = true;
            try
            {
                if (operation.IsError)
                {
                    await _syncQueue.MarkPendingAsync(operation.QueueId, cancellationToken);
                }

                if (operation.EntityName == "clients")
                {
                    await _clientDataService.LoadAsync(cancellationToken);
                }
                else if (operation.EntityName == "suppliers")
                {
                    await _supplierDataService.LoadAsync(cancellationToken);
                }
                else if (operation.EntityName == "products")
                {
                    await _productDataService.LoadAsync(cancellationToken);
                }
                else if (operation.EntityName == "orders")
                {
                    await _orderDataService.LoadAsync(cancellationToken);
                }
                else if (operation.EntityName == "stock")
                {
                    await _stockDataService.LoadAsync(cancellationToken);
                }
                else if (operation.EntityName == "stock_operations")
                {
                    await _stockDataService.LoadAsync(cancellationToken);
                }
                else if (operation.EntityName == "ubicacions")
                {
                    await _locationDataService.LoadAsync(cancellationToken);
                }
                else if (operation.EntityName == "proveidors_lot")
                {
                    await _supplierLotDataService.LoadAsync(cancellationToken);
                }
                else if (operation.EntityName == "picking_lines")
                {
                    await _pickingDataService.LoadAsync(cancellationToken);
                }
                else if (operation.EntityName is AdminIdentityDataService.UsersEntityName or AdminIdentityDataService.RolesEntityName or AdminIdentityDataService.JobTitlesEntityName)
                {
                    await _adminIdentityDataService.LoadAsync(cancellationToken);
                }
            }
            finally
            {
                IsBusy = false;
            }

            await RefreshAsync(cancellationToken);
        }

        public async Task DiscardAsync(PendingSyncOperation operation, CancellationToken cancellationToken = default)
        {
            await _syncQueue.RemoveAsync(operation.QueueId, cancellationToken);
            await CleanLocalCacheAsync(operation, cancellationToken);
            await RefreshAsync(cancellationToken);
        }

        private async Task CleanLocalCacheAsync(PendingSyncOperation operation, CancellationToken cancellationToken)
        {
            if (operation.EntityName == "clients")
            {
                await CleanCacheAsync<ClientRecord>("clients/list/v1", operation, record => record.IdText, record =>
                {
                    record.SyncState = ClientSyncState.Synced;
                    record.SyncMessage = "Canvi pendent descartat";
                }, cancellationToken);
            }
            else if (operation.EntityName == "suppliers")
            {
                await CleanCacheAsync<SupplierRecord>("suppliers/list/v1", operation, record => record.IdText, record =>
                {
                    record.SyncState = SupplierSyncState.Synced;
                    record.SyncMessage = "Canvi pendent descartat";
                }, cancellationToken);
            }
            else if (operation.EntityName == "products")
            {
                await CleanCacheAsync<ProductRecord>("products/list/v1", operation, record => record.IdText, record =>
                {
                    record.SyncState = ProductSyncState.Synced;
                    record.SyncMessage = "Canvi pendent descartat";
                }, cancellationToken);
            }
            else if (operation.EntityName == "orders")
            {
                await CleanCacheAsync<OrderRecord>("orders/list/v1", operation, record => record.IdText, record =>
                {
                    record.SyncState = OrderSyncState.Synced;
                    record.SyncMessage = "Canvi pendent descartat";
                }, cancellationToken);
            }
            else if (operation.EntityName == "stock")
            {
                await CleanWarehouseCacheAsync<StockRecord>("warehouse/stock/list/v1", operation, cancellationToken);
            }
            else if (operation.EntityName == "stock_operations")
            {
                await CleanWarehouseCacheAsync<StockRecord>("warehouse/stock/list/v1", operation, cancellationToken);
            }
            else if (operation.EntityName == "ubicacions")
            {
                await CleanWarehouseCacheAsync<LocationRecord>("warehouse/locations/list/v1", operation, cancellationToken);
            }
            else if (operation.EntityName == "proveidors_lot")
            {
                await CleanWarehouseCacheAsync<SupplierLotRecord>("warehouse/lots/list/v1", operation, cancellationToken);
            }
            else if (operation.EntityName == "picking_lines")
            {
                await CleanPickingLineCacheAsync(operation, cancellationToken);
            }
            else if (operation.EntityName == AdminIdentityDataService.UsersEntityName)
            {
                await CleanCacheAsync<AdminUserRecord>("admin/users/list/v1", operation, record => record.IdText, record =>
                {
                    record.SyncState = AdminIdentitySyncState.Synced;
                    record.SyncMessage = "Canvi pendent descartat";
                }, cancellationToken);
            }
            else if (operation.EntityName == AdminIdentityDataService.RolesEntityName)
            {
                await CleanCacheAsync<AdminSimpleRecord>("admin/roles/list/v1", operation, record => record.IdText, record =>
                {
                    record.SyncState = AdminIdentitySyncState.Synced;
                    record.SyncMessage = "Canvi pendent descartat";
                }, cancellationToken);
            }
            else if (operation.EntityName == AdminIdentityDataService.JobTitlesEntityName)
            {
                await CleanCacheAsync<AdminSimpleRecord>("admin/carrecs/list/v1", operation, record => record.IdText, record =>
                {
                    record.SyncState = AdminIdentitySyncState.Synced;
                    record.SyncMessage = "Canvi pendent descartat";
                }, cancellationToken);
            }
        }

        private async Task CleanPickingLineCacheAsync(PendingSyncOperation operation, CancellationToken cancellationToken)
        {
            await _localDatabase.InitializeAsync(cancellationToken);
            var states = await _localDatabase.GetJsonAsync<List<PickingLineCacheCleanupRecord>>("operations/picking/line-state/v1", cancellationToken);
            if (states is null)
            {
                return;
            }

            var state = states.FirstOrDefault(item => item.LineId.ToString(System.Globalization.CultureInfo.InvariantCulture) == operation.EntityId);
            if (state is not null)
            {
                state.SyncState = PickingLineSyncState.Synced;
                state.SyncMessage = "Canvi pendent descartat";
            }

            await _localDatabase.SetJsonAsync("operations/picking/line-state/v1", states, cancellationToken);
        }

        private async Task CleanWarehouseCacheAsync<T>(
            string cacheKey,
            PendingSyncOperation operation,
            CancellationToken cancellationToken)
            where T : IWarehouseRecord
        {
            await CleanCacheAsync<T>(cacheKey, operation, record => record.IdText, record =>
            {
                record.SyncState = WarehouseSyncState.Synced;
                record.SyncMessage = "Canvi pendent descartat";
            }, cancellationToken);
        }

        private async Task CleanCacheAsync<T>(
            string cacheKey,
            PendingSyncOperation operation,
            Func<T, string> idSelector,
            Action<T> markSynced,
            CancellationToken cancellationToken)
        {
            await _localDatabase.InitializeAsync(cancellationToken);
            var records = await _localDatabase.GetJsonAsync<List<T>>(cacheKey, cancellationToken);
            if (records is null)
            {
                return;
            }

            if (operation.Method == "POST")
            {
                records.RemoveAll(record => idSelector(record) == operation.EntityId);
            }
            else
            {
                var record = records.FirstOrDefault(record => idSelector(record) == operation.EntityId);
                if (record is not null)
                {
                    markSynced(record);
                }
            }

            await _localDatabase.SetJsonAsync(cacheKey, records, cancellationToken);
        }

        private static PendingSyncOperation ToOperation(SyncQueueItem item)
        {
            return new PendingSyncOperation
            {
                QueueId = item.Id,
                EntityName = item.EntityName,
                EntityLabel = EntityLabel(item.EntityName),
                EntityId = item.EntityId ?? "",
                Method = item.Method,
                OperationLabel = OperationLabel(item.Method),
                Status = item.Status,
                Endpoint = item.Endpoint,
                Payload = item.Payload,
                Summary = BuildSummary(item),
                UpdatedUtc = item.UpdatedUtc
            };
        }

        private static string EntityLabel(string entityName)
        {
            return entityName switch
            {
                "clients" => "Clients",
                "suppliers" => "Proveidors",
                "products" => "Productes",
                "orders" => "Comandes",
                "stock" => "Stock",
                "stock_operations" => "Operacions de stock",
                "ubicacions" => "Ubicacions",
                "proveidors_lot" => "Lots",
                "picking_lines" => "Linies de picking",
                "admin_users" => "Usuaris",
                "admin_roles" => "Rols",
                "admin_carrecs" => "Carrecs",
                _ => entityName
            };
        }

        private static string OperationLabel(string method)
        {
            return method switch
            {
                "POST" => "Crear",
                "PUT" => "Editar",
                "DELETE" => "Eliminar",
                "Entrada" => "Entrada",
                "Moviment" => "Moure",
                "Ajust" => "Ajust",
                "Reserva" => "Reservar",
                "Alliberament" => "Alliberar",
                _ => method
            };
        }

        private static string BuildSummary(SyncQueueItem item)
        {
            if (item.Method == "DELETE")
            {
                return $"{EntityLabel(item.EntityName)} #{item.EntityId}";
            }

            try
            {
                using var document = JsonDocument.Parse(item.Payload);
                var root = document.RootElement;
                var title = FirstString(root, "NomEmpresa", "Nom", "Referencia", "Email", "CodiGenerat");
                return string.IsNullOrWhiteSpace(title)
                    ? $"{EntityLabel(item.EntityName)} #{item.EntityId}"
                    : title!;
            }
            catch (JsonException)
            {
                return $"{EntityLabel(item.EntityName)} #{item.EntityId}";
            }
        }

        private static string? FirstString(JsonElement root, params string[] names)
        {
            foreach (var name in names)
            {
                if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
                {
                    return value.GetString();
                }
            }

            return null;
        }

        private void RaiseCounts()
        {
            OnPropertyChanged(nameof(HasItems));
            OnPropertyChanged(nameof(CountText));
        }

        private sealed class PickingLineCacheCleanupRecord
        {
            public int LineId { get; set; }
            public int? VerificationStateId { get; set; }
            public int? ReservedStockId { get; set; }
            public int ReservedBoxes { get; set; }
            public PickingLineState State { get; set; }
            public PickingLineSyncState SyncState { get; set; }
            public string SyncMessage { get; set; } = "";
        }
    }
}
