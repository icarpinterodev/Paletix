using PaletixDesktop.Models;
using SharedContracts.Dtos;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PaletixDesktop.Services
{
    public sealed record PickingLoadResult(
        IReadOnlyList<PickingOrderRecord> Orders,
        IReadOnlyList<PickingLineRecord> Lines,
        IReadOnlyList<StockRecord> Stock,
        bool IsOnline,
        int PendingCount,
        string Message,
        bool HasRequiredStates,
        string StateMessage);

    public sealed record PickingOperationResult(
        IReadOnlyList<PickingLineRecord> Lines,
        IReadOnlyList<StockRecord> Stock,
        int PendingCount,
        string Message);

    public sealed class PickingDataService
    {
        private const string OrdersCacheKey = "operations/picking/orders/v1";
        private const string LineStateCacheKey = "operations/picking/line-state/v1";
        private const string StatesCacheKey = "operations/picking/states/v1";
        private const string EntityName = "picking_lines";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly ApiClient _apiClient;
        private readonly LocalDatabase _localDatabase;
        private readonly SyncQueue _syncQueue;
        private readonly ComandaService _comandaService;
        private readonly StockDataService _stockDataService;
        private readonly LookupDataService _lookupDataService;

        public PickingDataService(
            ApiClient apiClient,
            LocalDatabase localDatabase,
            SyncQueue syncQueue,
            ComandaService comandaService,
            StockDataService stockDataService,
            LookupDataService lookupDataService)
        {
            _apiClient = apiClient;
            _localDatabase = localDatabase;
            _syncQueue = syncQueue;
            _comandaService = comandaService;
            _stockDataService = stockDataService;
            _lookupDataService = lookupDataService;
        }

        public async Task<PickingLoadResult> LoadAsync(CancellationToken cancellationToken = default)
        {
            await _localDatabase.InitializeAsync(cancellationToken);

            var stockResult = await _stockDataService.LoadAsync(cancellationToken);
            var stock = stockResult.Records.ToList();
            var isOnline = stockResult.IsOnline;
            var lineSyncErrors = 0;

            if (isOnline)
            {
                lineSyncErrors = await SynchronizePendingLineUpdatesAsync(cancellationToken);
            }

            var states = await LoadResolvedStatesAsync(isOnline, cancellationToken);
            var orders = await LoadOrdersAsync(isOnline, cancellationToken);
            var productLabels = await LoadProductLabelsAsync(cancellationToken);

            var built = BuildPickingData(orders, stock, productLabels, states);
            await ApplyActiveLineUpdatesAsync(built.Lines, states, cancellationToken);
            RefreshAvailability(built.Lines, stock);
            await SaveLineStateCacheAsync(built.Lines, cancellationToken);

            var pending = await _syncQueue.GetPendingCountAsync(cancellationToken);
            var source = isOnline ? "API" : "SQLite";
            var message = lineSyncErrors > 0
                ? $"{source} carregada amb {lineSyncErrors} error(s) de sincronitzacio de picking."
                : $"{source} carregada. {built.Orders.Count} comanda(es), {built.Lines.Count} linia(es).";

            return new PickingLoadResult(
                built.Orders,
                built.Lines,
                stock,
                isOnline,
                pending,
                message,
                states.HasRequiredStates,
                states.Message);
        }

        public async Task<PickingOperationResult> ReservePreparablesAsync(
            IReadOnlyList<PickingLineRecord> lines,
            IReadOnlyList<StockRecord> currentStock,
            CancellationToken cancellationToken = default)
        {
            var stock = currentStock.ToList();
            var reserved = 0;
            var skipped = 0;

            foreach (var line in lines.Where(line => line.State is PickingLineState.Ready && line.ReservedBoxes < line.RequestedBoxes).ToList())
            {
                var stockRecord = FindReservableStock(stock, line.ProductId, line.LocationId, line.RequestedBoxes);
                if (stockRecord is null)
                {
                    line.State = line.AvailableBoxes > 0 ? PickingLineState.Partial : PickingLineState.Missing;
                    skipped++;
                    continue;
                }

                var request = new StockReservaRequestDto
                {
                    IdStock = stockRecord.Id,
                    Quantitat = line.RequestedBoxes,
                    Motiu = $"Picking comanda {line.OrderId}, linia {line.LineId}"
                };

                var result = await _stockDataService.ApplyReservaAsync(request, stock, cancellationToken);
                stock = result.Records.ToList();
                line.ReservedStockId = stockRecord.Id;
                line.ReservedBoxes = line.RequestedBoxes;
                line.State = PickingLineState.Ready;
                reserved++;
            }

            RefreshAvailability(lines, stock);
            await SaveLineStateCacheAsync(lines, cancellationToken);
            var pending = await _syncQueue.GetPendingCountAsync(cancellationToken);
            var message = reserved == 0
                ? "No hi havia linies preparables per reservar."
                : $"{reserved} linia(es) reservades. {skipped} sense stock suficient.";
            return new PickingOperationResult(lines, stock, pending, message);
        }

        public async Task<PickingOperationResult> MarkPreparedAsync(
            IReadOnlyList<PickingLineRecord> selectedLines,
            IReadOnlyList<PickingLineRecord> allLines,
            IReadOnlyList<StockRecord> currentStock,
            CancellationToken cancellationToken = default)
        {
            var states = await LoadResolvedStatesAsync(allowApi: true, cancellationToken);
            if (!states.HasRequiredStates)
            {
                return await ResultAsync(allLines, currentStock, states.Message, cancellationToken);
            }

            var stock = currentStock.ToList();
            var updated = 0;
            foreach (var line in selectedLines)
            {
                if (line.State is PickingLineState.Incident)
                {
                    continue;
                }

                if (line.ReservedBoxes < line.RequestedBoxes)
                {
                    var stockRecord = FindReservableStock(stock, line.ProductId, line.LocationId, line.RequestedBoxes);
                    if (stockRecord is null)
                    {
                        line.State = line.AvailableBoxes > 0 ? PickingLineState.Partial : PickingLineState.Missing;
                        continue;
                    }

                    var reserve = await _stockDataService.ApplyReservaAsync(
                        new StockReservaRequestDto
                        {
                            IdStock = stockRecord.Id,
                            Quantitat = line.RequestedBoxes,
                            Motiu = $"Reserva automatica per preparar comanda {line.OrderId}, linia {line.LineId}"
                        },
                        stock,
                        cancellationToken);
                    stock = reserve.Records.ToList();
                    line.ReservedStockId = stockRecord.Id;
                    line.ReservedBoxes = line.RequestedBoxes;
                }

                if (line.ReservedBoxes < line.RequestedBoxes)
                {
                    continue;
                }

                await UpdateLineVerificationAsync(line, states.PreparedId!.Value, "Linia marcada com preparada", cancellationToken);
                line.State = PickingLineState.Prepared;
                updated++;
            }

            RefreshAvailability(allLines, stock);
            await SaveLineStateCacheAsync(allLines, cancellationToken);
            return await ResultAsync(allLines, stock, $"{updated} linia(es) marcades com preparades.", cancellationToken);
        }

        public async Task<PickingOperationResult> RegisterIncidentAsync(
            IReadOnlyList<PickingLineRecord> selectedLines,
            IReadOnlyList<PickingLineRecord> allLines,
            IReadOnlyList<StockRecord> currentStock,
            string reason,
            CancellationToken cancellationToken = default)
        {
            var states = await LoadResolvedStatesAsync(allowApi: true, cancellationToken);
            if (!states.HasRequiredStates)
            {
                return await ResultAsync(allLines, currentStock, states.Message, cancellationToken);
            }

            var updated = 0;
            foreach (var line in selectedLines)
            {
                await UpdateLineVerificationAsync(line, states.IncidentId!.Value, $"Incidencia: {reason.Trim()}", cancellationToken);
                line.State = PickingLineState.Incident;
                updated++;
            }

            await SaveLineStateCacheAsync(allLines, cancellationToken);
            return await ResultAsync(allLines, currentStock, $"{updated} incidencia(es) registrades.", cancellationToken);
        }

        public async Task<PickingOperationResult> ReleaseReservationsAsync(
            IReadOnlyList<PickingLineRecord> selectedLines,
            IReadOnlyList<PickingLineRecord> allLines,
            IReadOnlyList<StockRecord> currentStock,
            CancellationToken cancellationToken = default)
        {
            var stock = currentStock.ToList();
            var released = 0;
            foreach (var line in selectedLines.Where(line => line.HasReservation && line.State is not PickingLineState.Prepared).ToList())
            {
                var request = new StockAlliberamentRequestDto
                {
                    IdStock = line.ReservedStockId!.Value,
                    Quantitat = line.ReservedBoxes,
                    Motiu = $"Alliberament picking comanda {line.OrderId}, linia {line.LineId}"
                };

                var result = await _stockDataService.ApplyAlliberamentAsync(request, stock, cancellationToken);
                stock = result.Records.ToList();
                line.ReservedStockId = null;
                line.ReservedBoxes = 0;
                released++;
            }

            RefreshAvailability(allLines, stock);
            await SaveLineStateCacheAsync(allLines, cancellationToken);
            return await ResultAsync(allLines, stock, $"{released} reserva(es) alliberades.", cancellationToken);
        }

        public async Task<int> SynchronizePendingLineUpdatesAsync(CancellationToken cancellationToken = default)
        {
            await _localDatabase.InitializeAsync(cancellationToken);
            var pending = await _syncQueue.GetPendingAsync(EntityName, cancellationToken);
            var errors = 0;

            foreach (var item in pending)
            {
                try
                {
                    if (item.Method == "PUT")
                    {
                        var request = JsonSerializer.Deserialize<LiniescomandaRequestDto>(item.Payload, JsonOptions)
                            ?? throw new InvalidOperationException("Payload de linia de picking invalid.");
                        await _apiClient.PutAsync(item.Endpoint, request, cancellationToken);
                    }

                    await _syncQueue.MarkCompletedAsync(item.Id, cancellationToken);
                }
                catch (Exception ex) when (IsConnectivityFailure(ex))
                {
                    break;
                }
                catch (Exception)
                {
                    await _syncQueue.MarkFailedAsync(item.Id, cancellationToken);
                    errors++;
                }
            }

            return errors;
        }

        private async Task UpdateLineVerificationAsync(
            PickingLineRecord line,
            int stateId,
            string message,
            CancellationToken cancellationToken)
        {
            var request = new LiniescomandaRequestDto
            {
                IdComanda = line.OrderId,
                IdProducte = line.ProductId,
                IdUbicacio = line.LocationId,
                Palets = line.Pallets,
                Caixes = line.RequestedBoxes,
                IdEstatVerificacio = stateId
            };
            var endpoint = $"api/Liniescomandas/{line.LineId}";

            try
            {
                await _apiClient.PutAsync(endpoint, request, cancellationToken);
                line.VerificationStateId = stateId;
                line.SyncState = PickingLineSyncState.Synced;
                line.SyncMessage = "Sincronitzat";
                await _syncQueue.RemoveForEntityAsync(EntityName, line.LineId.ToString(CultureInfo.InvariantCulture), cancellationToken);
            }
            catch (Exception ex) when (IsConnectivityFailure(ex))
            {
                line.VerificationStateId = stateId;
                line.SyncState = PickingLineSyncState.Pending;
                line.SyncMessage = message;
                await _syncQueue.UpsertAsync(EntityName, line.LineId.ToString(CultureInfo.InvariantCulture), "PUT", endpoint, request, "Pending", cancellationToken);
            }
            catch (Exception ex)
            {
                line.VerificationStateId = stateId;
                line.SyncState = PickingLineSyncState.Error;
                line.SyncMessage = ex.Message;
                await _syncQueue.UpsertAsync(EntityName, line.LineId.ToString(CultureInfo.InvariantCulture), "PUT", endpoint, request, "Error", cancellationToken);
            }
        }

        private async Task<PickingOperationResult> ResultAsync(
            IReadOnlyList<PickingLineRecord> lines,
            IReadOnlyList<StockRecord> stock,
            string message,
            CancellationToken cancellationToken)
        {
            var pending = await _syncQueue.GetPendingCountAsync(cancellationToken);
            return new PickingOperationResult(lines, stock, pending, message);
        }

        private async Task<IReadOnlyList<ComandesReadDto>> LoadOrdersAsync(bool allowApi, CancellationToken cancellationToken)
        {
            if (allowApi)
            {
                try
                {
                    var page = await _comandaService.GetComandesAsync(1, 100, cancellationToken);
                    await _localDatabase.SetJsonAsync(OrdersCacheKey, page.Items, cancellationToken);
                    return page.Items;
                }
                catch (Exception ex) when (IsConnectivityFailure(ex))
                {
                }
            }

            return await _localDatabase.GetJsonAsync<List<ComandesReadDto>>(OrdersCacheKey, cancellationToken)
                ?? new List<ComandesReadDto>();
        }

        private async Task<IReadOnlyDictionary<int, string>> LoadProductLabelsAsync(CancellationToken cancellationToken)
        {
            try
            {
                var products = await _lookupDataService.GetProductsAsync(cancellationToken);
                return products
                    .Where(option => option.Id.HasValue)
                    .ToDictionary(option => option.Id!.Value, option => option.Label);
            }
            catch
            {
                return new Dictionary<int, string>();
            }
        }

        private async Task<ResolvedPickingStates> LoadResolvedStatesAsync(bool allowApi, CancellationToken cancellationToken)
        {
            if (allowApi)
            {
                try
                {
                    var page = await _apiClient.GetPagedAsync<EstatsReadDto>("api/Estats", 1, 200, cancellationToken);
                    var resolved = ResolveStates(page.Items);
                    await _localDatabase.SetJsonAsync(StatesCacheKey, resolved, cancellationToken);
                    return resolved;
                }
                catch (Exception ex) when (IsConnectivityFailure(ex))
                {
                }
            }

            return await _localDatabase.GetJsonAsync<ResolvedPickingStates>(StatesCacheKey, cancellationToken)
                ?? ResolvedPickingStates.Missing;
        }

        private static (List<PickingOrderRecord> Orders, List<PickingLineRecord> Lines) BuildPickingData(
            IReadOnlyList<ComandesReadDto> orders,
            IReadOnlyList<StockRecord> stock,
            IReadOnlyDictionary<int, string> productLabels,
            ResolvedPickingStates states)
        {
            var builtOrders = new List<PickingOrderRecord>();
            var builtLines = new List<PickingLineRecord>();
            var availableByProductLocation = AvailableByProductLocation(stock);

            foreach (var order in orders.OrderBy(order => order.DataPrevistaEntrega).ThenBy(order => order.Id))
            {
                var orderLines = new List<PickingLineRecord>();
                foreach (var line in order.Linies)
                {
                    var available = availableByProductLocation.TryGetValue((line.IdProducte, line.IdUbicacio), out var stockAvailable)
                        ? stockAvailable
                        : 0;
                    var state = StateFromVerification(line.IdEstatVerificacio, states)
                        ?? StateFromAvailability(available, line.Caixes);

                    var record = new PickingLineRecord
                    {
                        OrderId = order.Id,
                        LineId = line.Id,
                        ProductId = line.IdProducte,
                        LocationId = line.IdUbicacio,
                        RequestedBoxes = line.Caixes,
                        Pallets = line.Palets,
                        ProductText = productLabels.TryGetValue(line.IdProducte, out var label) ? label : $"Producte {line.IdProducte}",
                        LocationText = string.IsNullOrWhiteSpace(line.Ubicacio) ? $"Ubicacio {line.IdUbicacio}" : $"{line.IdUbicacio} · {line.Ubicacio}",
                        OrderState = string.IsNullOrWhiteSpace(order.Estat) ? $"Estat {order.IdEstat}" : order.Estat,
                    };
                    record.AvailableBoxes = available;
                    record.VerificationStateId = line.IdEstatVerificacio;
                    record.State = state;
                    record.SyncState = PickingLineSyncState.Synced;
                    record.SyncMessage = "Sincronitzat";

                    orderLines.Add(record);
                    builtLines.Add(record);
                }

                builtOrders.Add(new PickingOrderRecord
                {
                    Id = order.Id,
                    IdClient = order.IdClient,
                    Estat = string.IsNullOrWhiteSpace(order.Estat) ? $"Estat {order.IdEstat}" : order.Estat,
                    DeliveryDate = order.DataPrevistaEntrega,
                    LineCount = orderLines.Count,
                    MissingCount = orderLines.Count(line => line.NeedsAttention)
                });
            }

            return (builtOrders, builtLines);
        }

        private async Task ApplyActiveLineUpdatesAsync(
            IReadOnlyList<PickingLineRecord> lines,
            ResolvedPickingStates states,
            CancellationToken cancellationToken)
        {
            var cached = await _localDatabase.GetJsonAsync<List<CachedPickingLineState>>(LineStateCacheKey, cancellationToken)
                ?? new List<CachedPickingLineState>();
            foreach (var cachedState in cached)
            {
                var line = lines.FirstOrDefault(item => item.LineId == cachedState.LineId);
                if (line is null)
                {
                    continue;
                }

                cachedState.ApplyTo(line, states);
            }

            var active = await _syncQueue.GetActiveAsync(EntityName, cancellationToken);
            foreach (var item in active)
            {
                if (!int.TryParse(item.EntityId, out var lineId))
                {
                    continue;
                }

                var line = lines.FirstOrDefault(line => line.LineId == lineId);
                if (line is null)
                {
                    continue;
                }

                try
                {
                    var request = JsonSerializer.Deserialize<LiniescomandaRequestDto>(item.Payload, JsonOptions);
                    line.VerificationStateId = request?.IdEstatVerificacio;
                    var state = StateFromVerification(line.VerificationStateId, states);
                    if (state.HasValue)
                    {
                        line.State = state.Value;
                    }
                }
                catch (JsonException)
                {
                }

                line.SyncState = item.Status == "Error" ? PickingLineSyncState.Error : PickingLineSyncState.Pending;
                line.SyncMessage = item.Status == "Error" ? "Error sync" : "Canvi pendent de sincronitzar";
            }
        }

        private async Task SaveLineStateCacheAsync(IReadOnlyList<PickingLineRecord> lines, CancellationToken cancellationToken)
        {
            var cached = lines.Select(CachedPickingLineState.FromLine).ToList();
            await _localDatabase.SetJsonAsync(LineStateCacheKey, cached, cancellationToken);
        }

        private static ResolvedPickingStates ResolveStates(IReadOnlyList<EstatsReadDto> states)
        {
            var prepared = states.FirstOrDefault(IsPreparedState);
            var incident = states.FirstOrDefault(IsIncidentState);
            return new ResolvedPickingStates(prepared?.Id, incident?.Id);
        }

        private static bool IsPreparedState(EstatsReadDto state)
        {
            return NormalizeStateToken(state.Codi) == "PREPARAT" || NormalizeStateToken(state.Descripcio) == "PREPARAT";
        }

        private static bool IsIncidentState(EstatsReadDto state)
        {
            return NormalizeStateToken(state.Codi) == "INCIDENCIA" || NormalizeStateToken(state.Descripcio) == "INCIDENCIA";
        }

        private static string NormalizeStateToken(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "";
            }

            var normalized = text.Trim().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);
            foreach (var character in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(char.ToUpperInvariant(character));
                }
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }

        private static void RefreshAvailability(IReadOnlyList<PickingLineRecord> lines, IReadOnlyList<StockRecord> stock)
        {
            var availableByProductLocation = AvailableByProductLocation(stock);
            foreach (var line in lines)
            {
                line.AvailableBoxes = availableByProductLocation.TryGetValue((line.ProductId, line.LocationId), out var available)
                    ? available
                    : 0;

                if (line.State is PickingLineState.Prepared or PickingLineState.Incident)
                {
                    continue;
                }

                if (line.HasReservation)
                {
                    line.State = PickingLineState.Ready;
                    continue;
                }

                line.State = StateFromAvailability(line.AvailableBoxes, line.RequestedBoxes);
            }
        }

        private static Dictionary<(int ProductId, int LocationId), int> AvailableByProductLocation(IReadOnlyList<StockRecord> stock)
        {
            return stock
                .Where(record => !record.IsPendingDelete)
                .GroupBy(record => (record.IdProducte, record.IdUbicacio))
                .ToDictionary(
                    group => group.Key,
                    group => group.Sum(record => record.Disponibles ?? Math.Max(0, record.TotalsEnStock - record.ReservatsPerComandes)));
        }

        private static StockRecord? FindReservableStock(
            IReadOnlyList<StockRecord> stock,
            int productId,
            int locationId,
            int quantity)
        {
            return stock
                .Where(record => record.IdProducte == productId && record.IdUbicacio == locationId && !record.IsPendingDelete)
                .OrderBy(record => record.IdLot.HasValue ? 0 : 1)
                .ThenBy(record => record.Id)
                .FirstOrDefault(record => (record.Disponibles ?? Math.Max(0, record.TotalsEnStock - record.ReservatsPerComandes)) >= quantity);
        }

        private static PickingLineState StateFromAvailability(int available, int requested)
        {
            return available <= 0
                ? PickingLineState.Missing
                : available < requested
                    ? PickingLineState.Partial
                    : PickingLineState.Ready;
        }

        private static PickingLineState? StateFromVerification(int? verificationStateId, ResolvedPickingStates states)
        {
            if (!verificationStateId.HasValue)
            {
                return null;
            }

            if (states.PreparedId == verificationStateId.Value)
            {
                return PickingLineState.Prepared;
            }

            if (states.IncidentId == verificationStateId.Value)
            {
                return PickingLineState.Incident;
            }

            return null;
        }

        private static bool IsConnectivityFailure(Exception ex)
        {
            return ex is HttpRequestException or TaskCanceledException;
        }

        public sealed record ResolvedPickingStates(int? PreparedId, int? IncidentId)
        {
            public static ResolvedPickingStates Missing { get; } = new(null, null);
            public bool HasRequiredStates => PreparedId.HasValue && IncidentId.HasValue;
            public string Message => HasRequiredStates
                ? "Estats de picking configurats."
                : "Falten els estats PREPARAT/Preparat i INCIDENCIA/Incidencia a la taula estats. Configura'ls abans de persistir verificacions.";
        }

        public sealed record CachedPickingLineState(
            int LineId,
            int? VerificationStateId,
            int? ReservedStockId,
            int ReservedBoxes,
            PickingLineState State,
            PickingLineSyncState SyncState,
            string SyncMessage)
        {
            public static CachedPickingLineState FromLine(PickingLineRecord line)
            {
                return new CachedPickingLineState(
                    line.LineId,
                    line.VerificationStateId,
                    line.ReservedStockId,
                    line.ReservedBoxes,
                    line.State,
                    line.SyncState,
                    line.SyncMessage);
            }

            public void ApplyTo(PickingLineRecord line, ResolvedPickingStates states)
            {
                line.VerificationStateId = VerificationStateId;
                line.ReservedStockId = ReservedStockId;
                line.ReservedBoxes = ReservedBoxes;
                line.SyncState = SyncState;
                line.SyncMessage = SyncMessage;
                line.State = StateFromVerification(VerificationStateId, states) ?? State;
            }
        }
    }
}
