using PaletixDesktop.Models;
using PaletixDesktop.Services;
using SharedContracts.Dtos;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using static PaletixDesktop.ViewModels.WarehouseViewModelHelpers;

namespace PaletixDesktop.ViewModels
{
    public enum WarehouseCatalogViewMode
    {
        Table,
        Grid,
        Designer
    }

    public enum WarehousePanelMode
    {
        None,
        Create,
        Edit
    }

    public enum StockOperationPanelMode
    {
        None,
        Entrada,
        Moviment,
        Ajust,
        Reserva,
        Alliberament
    }

    public abstract class WarehouseCatalogViewModel<TRecord> : ViewModelBase
        where TRecord : class, IWarehouseRecord
    {
        private readonly IWarehouseDataService<TRecord> _dataService;
        private readonly ShellViewModel _shell;
        private WarehouseCatalogViewMode _viewMode = WarehouseCatalogViewMode.Table;
        private WarehousePanelMode _panelMode = WarehousePanelMode.None;
        private string _searchText = "";
        private string _statusText = "Carregant dades...";
        private string _validationMessage = "";
        private int _editIndex;

        protected WarehouseCatalogViewModel(IWarehouseDataService<TRecord> dataService, ShellViewModel shell)
        {
            _dataService = dataService;
            _shell = shell;
        }

        public ObservableCollection<TRecord> Records { get; } = new();
        public ObservableCollection<TRecord> FilteredRecords { get; } = new();
        public ObservableCollection<TRecord> SelectedRecords { get; } = new();

        public bool IsTableView => ViewMode == WarehouseCatalogViewMode.Table;
        public bool IsGridView => ViewMode == WarehouseCatalogViewMode.Grid;
        public bool IsDesignerView => ViewMode == WarehouseCatalogViewMode.Designer;
        public bool IsPanelOpen => PanelMode != WarehousePanelMode.None;
        public bool IsEditingMultiple => PanelMode == WarehousePanelMode.Edit && SelectedRecords.Count > 1;
        public string PanelTitle => PanelMode == WarehousePanelMode.Edit ? $"Editar {SingularNameLower}" : $"Nou {SingularNameLower}";
        public string PanelSubtitle => PanelMode == WarehousePanelMode.Edit ? $"{SingularName} {EditPositionText}" : "Alta preparada per API, SQLite i mode offline.";
        public string EditPositionText => SelectedRecords.Count == 0 ? "" : $"{_editIndex + 1} de {SelectedRecords.Count}";

        public WarehouseCatalogViewMode ViewMode
        {
            get => _viewMode;
            private set
            {
                if (SetProperty(ref _viewMode, value))
                {
                    OnPropertyChanged(nameof(IsTableView));
                    OnPropertyChanged(nameof(IsGridView));
                    OnPropertyChanged(nameof(IsDesignerView));
                }
            }
        }

        public WarehousePanelMode PanelMode
        {
            get => _panelMode;
            private set
            {
                if (SetProperty(ref _panelMode, value))
                {
                    OnPropertyChanged(nameof(IsPanelOpen));
                    OnPropertyChanged(nameof(IsEditingMultiple));
                    OnPropertyChanged(nameof(PanelTitle));
                    OnPropertyChanged(nameof(PanelSubtitle));
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    RefreshFilter();
                }
            }
        }

        public string StatusText { get => _statusText; protected set => SetProperty(ref _statusText, value); }

        public string ValidationMessage
        {
            get => _validationMessage;
            protected set
            {
                if (SetProperty(ref _validationMessage, value))
                {
                    OnPropertyChanged(nameof(HasValidationErrors));
                }
            }
        }

        public bool HasValidationErrors => !string.IsNullOrWhiteSpace(ValidationMessage);
        public int TotalCount => Records.Count;
        public int FilteredCount => FilteredRecords.Count;
        public int SelectedCount => SelectedRecords.Count;
        public int PendingCount => Records.Count(record => record.IsPending);
        public string SelectedCountText => $"{SelectedCount} seleccionat(s)";

        protected abstract string SingularName { get; }
        protected abstract string SingularNameLower { get; }
        protected abstract bool MatchesSearch(TRecord record, string query);
        protected abstract TRecord CreateEmptyDraft();
        protected abstract TRecord CreateFromDraft(int id);
        protected abstract void LoadDraft(TRecord record);
        protected abstract void SaveDraftInto(TRecord record);
        protected abstract bool ValidateDraft();

        protected virtual Task LoadLookupsAsync() => Task.CompletedTask;
        protected virtual void ApplyLookups(TRecord record) { }
        protected virtual IEnumerable<TRecord> SortRecords(IEnumerable<TRecord> records) => records.OrderByDescending(record => record.Id < 0).ThenBy(record => record.Id);
        protected virtual void RaiseSpecificMetrics() { }

        public async Task LoadAsync()
        {
            IsBusy = true;
            ErrorMessage = null;

            try
            {
                await LoadLookupsAsync();

                var cached = await _dataService.LoadCachedAsync();
                if (cached.Records.Count > 0)
                {
                    ReplaceRecords(cached.Records);
                    StatusText = cached.Message;
                }

                var result = await _dataService.LoadAsync();
                ReplaceRecords(result.Records);
                StatusText = result.Message;
                await _shell.RefreshSyncStatusAsync(result.IsOnline, result.IsOnline ? "Sincronitzat" : "Mode offline");
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                StatusText = $"No s'han pogut carregar dades: {ex.Message}";
                await _shell.RefreshSyncStatusAsync(false, "Mode offline");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void SetViewMode(WarehouseCatalogViewMode mode)
        {
            ViewMode = mode;
            StatusText = mode switch
            {
                WarehouseCatalogViewMode.Table => "Vista de taula activada.",
                WarehouseCatalogViewMode.Designer => "Dissenyador visual activat.",
                _ => "Vista de grid activada."
            };
        }

        public void SetSelection(IEnumerable<TRecord> records)
        {
            foreach (var record in Records)
            {
                record.IsSelected = false;
            }

            SelectedRecords.Clear();
            foreach (var record in records.Distinct())
            {
                record.IsSelected = true;
                SelectedRecords.Add(record);
            }

            if (_editIndex >= SelectedRecords.Count)
            {
                _editIndex = Math.Max(0, SelectedRecords.Count - 1);
            }

            RaiseSelectionProperties();
            OnSelectionChanged();
        }

        public void SelectSingle(TRecord record)
        {
            SetSelection(new[] { record });
        }

        public void CreateRecord()
        {
            OpenCreatePanel(CreateEmptyDraft(), $"Introdueix les dades del nou {SingularNameLower} al panell lateral.");
        }

        public void StartEditSelected()
        {
            if (SelectedRecords.Count == 0)
            {
                StatusText = $"Selecciona {SingularNameLower} abans d'editar.";
                return;
            }

            _editIndex = 0;
            LoadDraft(SelectedRecords[_editIndex]);
            PanelMode = WarehousePanelMode.Edit;
            RaiseSelectionProperties();
        }

        public void MoveEdit(int delta)
        {
            if (SelectedRecords.Count == 0)
            {
                return;
            }

            SaveDraftInto(SelectedRecords[_editIndex]);
            _editIndex = (_editIndex + delta + SelectedRecords.Count) % SelectedRecords.Count;
            LoadDraft(SelectedRecords[_editIndex]);
            OnPropertyChanged(nameof(EditPositionText));
            OnPropertyChanged(nameof(PanelSubtitle));
        }

        public async Task SavePanelAsync()
        {
            if (!ValidateDraft())
            {
                StatusText = "Revisa els camps del formulari abans de guardar.";
                return;
            }

            if (PanelMode == WarehousePanelMode.Create)
            {
                var draft = CreateFromDraft(0);
                var result = await _dataService.CreateAsync(draft, Records.ToList());
                ApplyMutation(result);
                if (result.Record is not null)
                {
                    SelectSingle(Records.First(record => record.Id == result.Record.Id));
                }
            }
            else if (PanelMode == WarehousePanelMode.Edit && SelectedRecords.Count > 0)
            {
                SaveDraftInto(SelectedRecords[_editIndex]);
                var selectedIds = SelectedRecords.Select(record => record.Id).ToList();
                WarehouseMutationResult<TRecord>? lastResult = null;

                foreach (var selected in SelectedRecords.ToList())
                {
                    var current = Records.FirstOrDefault(record => record.Id == selected.Id) ?? selected;
                    lastResult = await _dataService.UpdateAsync(current, Records.ToList());
                    ApplyMutation(lastResult, selectedIds);
                }

                if (lastResult is not null)
                {
                    StatusText = lastResult.Message;
                }
            }

            PanelMode = WarehousePanelMode.None;
            ValidationMessage = "";
        }

        public void CancelPanel()
        {
            PanelMode = WarehousePanelMode.None;
            ValidationMessage = "";
            StatusText = "Edicio cancelada.";
        }

        public async Task DeleteSelectedAsync()
        {
            if (SelectedRecords.Count == 0)
            {
                StatusText = $"Selecciona {SingularNameLower} abans d'eliminar.";
                return;
            }

            WarehouseMutationResult<TRecord>? lastResult = null;
            foreach (var selected in SelectedRecords.ToList())
            {
                var current = Records.FirstOrDefault(record => record.Id == selected.Id) ?? selected;
                lastResult = await _dataService.DeleteAsync(current, Records.ToList());
                ApplyMutation(lastResult);
            }

            SelectedRecords.Clear();
            RaiseSelectionProperties();
            if (lastResult is not null)
            {
                StatusText = lastResult.Message;
            }
        }

        protected void SetDraftProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (SetProperty(ref storage, value, propertyName) && IsPanelOpen)
            {
                ValidateDraft();
            }
        }

        protected static double ToNumberValue(int value) => value;
        protected static int FromNumberValue(double value, int fallback = 0) => double.IsNaN(value) ? fallback : Math.Max(0, Convert.ToInt32(value));

        protected static void Require(string value, string label, ICollection<string> errors)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add($"{label} es obligatori.");
            }
        }

        protected static void ValidateRequiredLookup(string value, IEnumerable<LookupOption> options, string label, ICollection<string> errors)
        {
            if (string.IsNullOrWhiteSpace(value) || !options.Any(option => option.Value == value))
            {
                errors.Add($"{label} s'ha de triar del desplegable.");
            }
        }

        protected void OpenCreatePanel(TRecord draft, string statusText)
        {
            LoadDraft(draft);
            PanelMode = WarehousePanelMode.Create;
            StatusText = statusText;
        }

        protected void ApplyMutation(WarehouseMutationResult<TRecord> result, IReadOnlyList<int>? selectedIds = null)
        {
            ReplaceRecords(result.Records);
            StatusText = result.Message;
            if (selectedIds is not null)
            {
                SetSelection(Records.Where(record => selectedIds.Contains(record.Id)));
            }

            _ = _shell.RefreshSyncStatusAsync(null, result.PendingCount == 0 ? "Sincronitzat" : result.Message);
        }

        private void ReplaceRecords(IReadOnlyList<TRecord> records)
        {
            Records.Clear();
            foreach (var record in SortRecords(records))
            {
                ApplyLookups(record);
                Records.Add(record);
            }

            RefreshFilter();
            RaiseMetrics();
            OnRecordsChanged();
        }

        private void RefreshFilter()
        {
            var query = SearchText.Trim().ToLowerInvariant();
            var records = string.IsNullOrWhiteSpace(query)
                ? Records
                : Records.Where(record => MatchesSearch(record, query));

            FilteredRecords.Clear();
            foreach (var record in records)
            {
                FilteredRecords.Add(record);
            }

            SetSelection(SelectedRecords.Where(FilteredRecords.Contains).ToList());
            RaiseMetrics();
            OnFilterChanged();
        }

        private void RaiseMetrics()
        {
            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(FilteredCount));
            OnPropertyChanged(nameof(PendingCount));
            RaiseSpecificMetrics();
        }

        private void RaiseSelectionProperties()
        {
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(SelectedCountText));
            OnPropertyChanged(nameof(IsEditingMultiple));
            OnPropertyChanged(nameof(EditPositionText));
            OnPropertyChanged(nameof(PanelSubtitle));
        }

        protected virtual void OnRecordsChanged() { }
        protected virtual void OnFilterChanged() { }
        protected virtual void OnSelectionChanged() { }
    }

    public sealed class StockCatalogViewModel : WarehouseCatalogViewModel<StockRecord>
    {
        private readonly StockDataService _dataService;
        private readonly LookupDataService _lookups;
        private readonly LocationDataService _locationDataService;
        private string _draftIdProducte = "";
        private string _draftIdUbicacio = "";
        private string _draftIdLot = "";
        private double _draftTotalsEnStockValue;
        private double _draftReservatsPerComandesValue;
        private bool _isLocationPickerOpen;
        private string _locationPickerSearchText = "";
        private StockOperationPanelMode _operationPanelMode = StockOperationPanelMode.None;
        private string _operationIdProducte = "";
        private string _operationIdUbicacio = "";
        private string _operationIdUbicacioOrigen = "";
        private string _operationIdUbicacioDesti = "";
        private string _operationIdLot = "";
        private double _operationQuantitatValue = 1;
        private double _operationNouTotalValue;
        private string _operationMotiu = "";
        private bool _isHistoryOpen;

        public StockCatalogViewModel()
            : this(App.CurrentServices.StockDataService, App.CurrentServices.LookupDataService, App.CurrentServices.LocationDataService, App.CurrentServices.ShellViewModel)
        {
        }

        public StockCatalogViewModel(StockDataService dataService, LookupDataService lookups, LocationDataService locationDataService, ShellViewModel shell)
            : base(dataService, shell)
        {
            _dataService = dataService;
            _lookups = lookups;
            _locationDataService = locationDataService;
        }

        public ObservableCollection<LookupOption> ProductOptions { get; } = new();
        public ObservableCollection<LookupOption> LocationOptions { get; } = new();
        public ObservableCollection<LookupOption> LotOptions { get; } = new();
        public ObservableCollection<LocationRecord> LocationPickerRecords { get; } = new();
        public ObservableCollection<LocationDesignerZone> LocationPickerZones { get; } = new();
        public ObservableCollection<StockMovementRecord> HistoryRecords { get; } = new();

        public string DraftIdProducte { get => _draftIdProducte; set => SetDraftProperty(ref _draftIdProducte, value); }
        public string DraftIdUbicacio
        {
            get => _draftIdUbicacio;
            set
            {
                SetDraftProperty(ref _draftIdUbicacio, value);
                OnPropertyChanged(nameof(SelectedLocationText));
                if (IsLocationPickerOpen)
                {
                    RebuildLocationPicker();
                }
            }
        }
        public string DraftIdLot { get => _draftIdLot; set => SetDraftProperty(ref _draftIdLot, value); }
        public double DraftTotalsEnStockValue { get => _draftTotalsEnStockValue; set => SetDraftProperty(ref _draftTotalsEnStockValue, value); }
        public double DraftReservatsPerComandesValue { get => _draftReservatsPerComandesValue; set => SetDraftProperty(ref _draftReservatsPerComandesValue, value); }
        public bool IsLocationPickerOpen { get => _isLocationPickerOpen; private set => SetProperty(ref _isLocationPickerOpen, value); }
        public string LocationPickerSearchText
        {
            get => _locationPickerSearchText;
            set
            {
                if (SetProperty(ref _locationPickerSearchText, value))
                {
                    RebuildLocationPicker();
                }
            }
        }

        public string SelectedLocationText => LookupText(LocationOptions, ParseInt(DraftIdUbicacio, 0), "Cap ubicacio seleccionada");
        public bool IsOperationPanelOpen => OperationPanelMode != StockOperationPanelMode.None;
        public bool IsEntradaMode => OperationPanelMode == StockOperationPanelMode.Entrada;
        public bool IsMovimentMode => OperationPanelMode == StockOperationPanelMode.Moviment;
        public bool IsAjustMode => OperationPanelMode == StockOperationPanelMode.Ajust;
        public bool IsQuantityOperationMode => OperationPanelMode is StockOperationPanelMode.Entrada or StockOperationPanelMode.Moviment or StockOperationPanelMode.Reserva or StockOperationPanelMode.Alliberament;
        public bool IsSelectedStockOperationMode => OperationPanelMode is StockOperationPanelMode.Ajust or StockOperationPanelMode.Reserva or StockOperationPanelMode.Alliberament;
        public bool IsHistoryOpen { get => _isHistoryOpen; private set => SetProperty(ref _isHistoryOpen, value); }
        public string OperationPanelTitle => OperationPanelMode switch
        {
            StockOperationPanelMode.Entrada => "Entrada de stock",
            StockOperationPanelMode.Moviment => "Moure stock",
            StockOperationPanelMode.Ajust => "Ajust inventari",
            StockOperationPanelMode.Reserva => "Reservar stock",
            StockOperationPanelMode.Alliberament => "Alliberar reserva",
            _ => ""
        };
        public string OperationSelectedStockText => SelectedRecords.Count == 0
            ? "Cap stock seleccionat"
            : $"{SelectedRecords[0].ProducteText} · {SelectedRecords[0].UbicacioText} · Disponible {SelectedRecords[0].DisponiblesText}";
        public StockOperationPanelMode OperationPanelMode
        {
            get => _operationPanelMode;
            private set
            {
                if (SetProperty(ref _operationPanelMode, value))
                {
                    OnPropertyChanged(nameof(IsOperationPanelOpen));
                    OnPropertyChanged(nameof(IsEntradaMode));
                    OnPropertyChanged(nameof(IsMovimentMode));
                    OnPropertyChanged(nameof(IsAjustMode));
                    OnPropertyChanged(nameof(IsQuantityOperationMode));
                    OnPropertyChanged(nameof(IsSelectedStockOperationMode));
                    OnPropertyChanged(nameof(OperationPanelTitle));
                }
            }
        }

        public string OperationIdProducte { get => _operationIdProducte; set => SetOperationProperty(ref _operationIdProducte, value); }
        public string OperationIdUbicacio { get => _operationIdUbicacio; set => SetOperationProperty(ref _operationIdUbicacio, value); }
        public string OperationIdUbicacioOrigen { get => _operationIdUbicacioOrigen; set => SetOperationProperty(ref _operationIdUbicacioOrigen, value); }
        public string OperationIdUbicacioDesti { get => _operationIdUbicacioDesti; set => SetOperationProperty(ref _operationIdUbicacioDesti, value); }
        public string OperationIdLot { get => _operationIdLot; set => SetOperationProperty(ref _operationIdLot, value); }
        public double OperationQuantitatValue { get => _operationQuantitatValue; set => SetOperationProperty(ref _operationQuantitatValue, value); }
        public double OperationNouTotalValue { get => _operationNouTotalValue; set => SetOperationProperty(ref _operationNouTotalValue, value); }
        public string OperationMotiu { get => _operationMotiu; set => SetOperationProperty(ref _operationMotiu, value); }
        public int ReservedCount => Records.Sum(record => record.ReservatsPerComandes);
        public int LowAvailableCount => Records.Count(record => (record.Disponibles ?? record.TotalsEnStock - record.ReservatsPerComandes) <= 5);

        protected override string SingularName => "Stock";
        protected override string SingularNameLower => "registre de stock";

        protected override async Task LoadLookupsAsync()
        {
            await LoadLookupCollectionAsync(ProductOptions, await _lookups.GetProductsAsync());
            await LoadLookupCollectionAsync(LocationOptions, await _lookups.GetLocationsAsync());
            await LoadLookupCollectionAsync(LotOptions, await _lookups.GetLotsAsync(includeEmpty: true));
            await LoadLocationPickerRecordsAsync();
        }

        protected override void ApplyLookups(StockRecord record)
        {
            record.ProducteText = LookupText(ProductOptions, record.IdProducte, $"Producte {record.IdProducte}");
            record.UbicacioText = LookupText(LocationOptions, record.IdUbicacio, $"Ubicacio {record.IdUbicacio}");
            record.LotText = record.IdLot is null ? "Sense lot" : LookupText(LotOptions, record.IdLot.Value, $"Lot {record.IdLot}");
        }

        protected override bool MatchesSearch(StockRecord record, string query)
        {
            return record.IdText.Contains(query)
                || record.ProducteText.ToLowerInvariant().Contains(query)
                || record.UbicacioText.ToLowerInvariant().Contains(query)
                || record.LotText.ToLowerInvariant().Contains(query)
                || record.SyncStateText.ToLowerInvariant().Contains(query);
        }

        protected override StockRecord CreateEmptyDraft()
        {
            return new StockRecord
            {
                IdProducte = FirstLookupValue(ProductOptions),
                IdUbicacio = FirstLookupValue(LocationOptions),
                TotalsEnStock = 0,
                ReservatsPerComandes = 0,
                Disponibles = 0
            };
        }

        protected override void LoadDraft(StockRecord record)
        {
            ValidationMessage = "";
            DraftIdProducte = record.IdProducte.ToString(CultureInfo.InvariantCulture);
            DraftIdUbicacio = record.IdUbicacio.ToString(CultureInfo.InvariantCulture);
            DraftIdLot = record.IdLot?.ToString(CultureInfo.InvariantCulture) ?? "";
            DraftTotalsEnStockValue = record.TotalsEnStock;
            DraftReservatsPerComandesValue = record.ReservatsPerComandes;
            OnPropertyChanged(nameof(SelectedLocationText));
        }

        protected override bool ValidateDraft()
        {
            var errors = new List<string>();
            ValidateRequiredLookup(DraftIdProducte, ProductOptions, "Producte", errors);
            ValidateRequiredLookup(DraftIdUbicacio, LocationOptions, "Ubicacio", errors);
            if (!string.IsNullOrWhiteSpace(DraftIdLot) && !LotOptions.Any(option => option.Value == DraftIdLot))
            {
                errors.Add("Lot s'ha de triar del desplegable.");
            }

            if (DraftTotalsEnStockValue < 0)
            {
                errors.Add("Total en stock ha de ser igual o superior a 0.");
            }

            if (DraftReservatsPerComandesValue < 0)
            {
                errors.Add("Reservat per comandes ha de ser igual o superior a 0.");
            }

            if (DraftReservatsPerComandesValue > DraftTotalsEnStockValue)
            {
                errors.Add("Reservat per comandes no pot superar el total en stock.");
            }

            ValidationMessage = string.Join("\n", errors);
            return errors.Count == 0;
        }

        protected override StockRecord CreateFromDraft(int id)
        {
            var total = FromNumberValue(DraftTotalsEnStockValue);
            var reserved = FromNumberValue(DraftReservatsPerComandesValue);
            return new StockRecord
            {
                Id = id,
                IdProducte = ParseInt(DraftIdProducte, FirstLookupValue(ProductOptions)),
                IdUbicacio = ParseInt(DraftIdUbicacio, FirstLookupValue(LocationOptions)),
                IdLot = string.IsNullOrWhiteSpace(DraftIdLot) ? null : ParseInt(DraftIdLot, 0),
                TotalsEnStock = total,
                ReservatsPerComandes = reserved,
                Disponibles = Math.Max(0, total - reserved)
            };
        }

        protected override void SaveDraftInto(StockRecord record)
        {
            var updated = CreateFromDraft(record.Id);
            record.IdProducte = updated.IdProducte;
            record.IdUbicacio = updated.IdUbicacio;
            record.IdLot = updated.IdLot;
            record.TotalsEnStock = updated.TotalsEnStock;
            record.ReservatsPerComandes = updated.ReservatsPerComandes;
            record.Disponibles = updated.Disponibles;
            ApplyLookups(record);
        }

        public void OpenLocationPicker()
        {
            RebuildLocationPicker();
            IsLocationPickerOpen = true;
            StatusText = "Selecciona una ubicacio des del mapa visual o continua amb el desplegable.";
        }

        public void CancelLocationPicker()
        {
            IsLocationPickerOpen = false;
        }

        public void SelectLocationFromPicker(LocationDesignerCell cell)
        {
            if (cell.Record is null)
            {
                return;
            }

            var value = cell.Record.Id.ToString(CultureInfo.InvariantCulture);
            if (OperationPanelMode == StockOperationPanelMode.Entrada)
            {
                OperationIdUbicacio = value;
            }
            else if (OperationPanelMode == StockOperationPanelMode.Moviment)
            {
                OperationIdUbicacioDesti = value;
            }
            else
            {
                DraftIdUbicacio = value;
            }

            IsLocationPickerOpen = false;
            StatusText = $"Ubicacio seleccionada: {cell.Record.CodiText}.";
            OnPropertyChanged(nameof(SelectedLocationText));
        }

        public string GetOccupiedLocationDialogText(LocationDesignerCell cell)
        {
            var action = OperationPanelMode == StockOperationPanelMode.Moviment
                ? "moure stock cap a aquesta ubicacio"
                : "usar aquesta ubicacio";
            return $"La ubicacio {cell.CodeText} esta ocupada ({cell.OccupancyText}). Vols {action} igualment?";
        }

        public string OccupiedLocationPrimaryButtonText => OperationPanelMode == StockOperationPanelMode.Moviment
            ? "Moure igualment"
            : "Continuar igualment";

        public void OpenEntrada()
        {
            CancelPanel();
            LoadOperationDefaults();
            OperationPanelMode = StockOperationPanelMode.Entrada;
            StatusText = "Registra entrada fisica de stock.";
        }

        public void OpenMoviment()
        {
            CancelPanel();
            LoadOperationDefaults();
            if (SelectedRecords.Count > 0)
            {
                var selected = SelectedRecords[0];
                OperationIdProducte = selected.IdProducte.ToString(CultureInfo.InvariantCulture);
                OperationIdLot = selected.IdLot?.ToString(CultureInfo.InvariantCulture) ?? "";
                OperationIdUbicacioOrigen = selected.IdUbicacio.ToString(CultureInfo.InvariantCulture);
            }

            OperationPanelMode = StockOperationPanelMode.Moviment;
            StatusText = "Mou stock disponible entre ubicacions.";
        }

        public void OpenAjust() => OpenSelectedOperation(StockOperationPanelMode.Ajust);
        public void OpenReserva() => OpenSelectedOperation(StockOperationPanelMode.Reserva);
        public void OpenAlliberament() => OpenSelectedOperation(StockOperationPanelMode.Alliberament);

        public void CancelOperationPanel()
        {
            OperationPanelMode = StockOperationPanelMode.None;
            ValidationMessage = "";
        }

        public async Task SaveOperationAsync()
        {
            if (!ValidateOperation())
            {
                StatusText = "Revisa els camps de l'operacio.";
                return;
            }

            try
            {
                WarehouseMutationResult<StockRecord> result;
                if (OperationPanelMode == StockOperationPanelMode.Entrada)
                {
                    result = await _dataService.ApplyEntradaAsync(new StockEntradaRequestDto
                    {
                        IdProducte = ParseInt(OperationIdProducte, FirstLookupValue(ProductOptions)),
                        IdUbicacio = ParseInt(OperationIdUbicacio, FirstLookupValue(LocationOptions)),
                        IdLot = string.IsNullOrWhiteSpace(OperationIdLot) ? null : ParseInt(OperationIdLot, 0),
                        Quantitat = Math.Max(1, FromNumberValue(OperationQuantitatValue, 1)),
                        Motiu = OperationMotiu
                    }, Records.ToList());
                }
                else if (OperationPanelMode == StockOperationPanelMode.Moviment)
                {
                    result = await _dataService.ApplyMovimentAsync(new StockMovimentRequestDto
                    {
                        IdProducte = ParseInt(OperationIdProducte, FirstLookupValue(ProductOptions)),
                        IdUbicacioOrigen = ParseInt(OperationIdUbicacioOrigen, FirstLookupValue(LocationOptions)),
                        IdUbicacioDesti = ParseInt(OperationIdUbicacioDesti, FirstLookupValue(LocationOptions)),
                        IdLot = string.IsNullOrWhiteSpace(OperationIdLot) ? null : ParseInt(OperationIdLot, 0),
                        Quantitat = Math.Max(1, FromNumberValue(OperationQuantitatValue, 1)),
                        Motiu = OperationMotiu
                    }, Records.ToList());
                }
                else
                {
                    var selected = SelectedRecords.First();
                    result = OperationPanelMode switch
                    {
                        StockOperationPanelMode.Ajust => await _dataService.ApplyAjustAsync(new StockAjustRequestDto { IdStock = selected.Id, NouTotal = Math.Max(0, FromNumberValue(OperationNouTotalValue, 0)), Motiu = OperationMotiu }, Records.ToList()),
                        StockOperationPanelMode.Reserva => await _dataService.ApplyReservaAsync(new StockReservaRequestDto { IdStock = selected.Id, Quantitat = Math.Max(1, FromNumberValue(OperationQuantitatValue, 1)), Motiu = OperationMotiu }, Records.ToList()),
                        StockOperationPanelMode.Alliberament => await _dataService.ApplyAlliberamentAsync(new StockAlliberamentRequestDto { IdStock = selected.Id, Quantitat = Math.Max(1, FromNumberValue(OperationQuantitatValue, 1)), Motiu = OperationMotiu }, Records.ToList()),
                        _ => throw new InvalidOperationException("Operacio no valida.")
                    };
                }

                ApplyMutation(result);
                OperationPanelMode = StockOperationPanelMode.None;
                ValidationMessage = "";
                await LoadHistoryAsync();
            }
            catch (Exception ex)
            {
                ValidationMessage = ex.Message;
                StatusText = "No s'ha pogut aplicar l'operacio.";
            }
        }

        public async Task OpenHistoryAsync()
        {
            IsHistoryOpen = true;
            await LoadHistoryAsync();
        }

        public void CloseHistory()
        {
            IsHistoryOpen = false;
        }

        protected override void RaiseSpecificMetrics()
        {
            OnPropertyChanged(nameof(ReservedCount));
            OnPropertyChanged(nameof(LowAvailableCount));
            OnPropertyChanged(nameof(OperationSelectedStockText));
        }

        protected override void OnSelectionChanged()
        {
            OnPropertyChanged(nameof(OperationSelectedStockText));
        }

        protected override void OnRecordsChanged()
        {
            RebuildLocationPicker();
        }

        private async Task LoadHistoryAsync()
        {
            try
            {
                var records = await _dataService.LoadMovimentsAsync();
                HistoryRecords.Clear();
                foreach (var record in records)
                {
                    record.ProducteText = LookupText(ProductOptions, record.IdProducte, $"Producte {record.IdProducte}");
                    record.LotText = record.IdLot is null ? "Sense lot" : LookupText(LotOptions, record.IdLot.Value, $"Lot {record.IdLot}");
                    record.OrigenText = record.IdUbicacioOrigen is null ? "-" : LookupText(LocationOptions, record.IdUbicacioOrigen.Value, $"Ubicacio {record.IdUbicacioOrigen}");
                    record.DestiText = record.IdUbicacioDesti is null ? "-" : LookupText(LocationOptions, record.IdUbicacioDesti.Value, $"Ubicacio {record.IdUbicacioDesti}");
                    HistoryRecords.Add(record);
                }
            }
            catch
            {
                StatusText = "No s'ha pogut carregar l'historial de stock.";
            }
        }

        private void OpenSelectedOperation(StockOperationPanelMode mode)
        {
            if (SelectedRecords.Count != 1)
            {
                StatusText = "Selecciona un unic registre de stock.";
                return;
            }

            CancelPanel();
            LoadOperationDefaults();
            var selected = SelectedRecords[0];
            OperationNouTotalValue = selected.TotalsEnStock;
            OperationPanelMode = mode;
            StatusText = OperationPanelTitle;
        }

        private void LoadOperationDefaults()
        {
            ValidationMessage = "";
            OperationIdProducte = FirstLookupValue(ProductOptions).ToString(CultureInfo.InvariantCulture);
            OperationIdUbicacio = FirstLookupValue(LocationOptions).ToString(CultureInfo.InvariantCulture);
            OperationIdUbicacioOrigen = FirstLookupValue(LocationOptions).ToString(CultureInfo.InvariantCulture);
            OperationIdUbicacioDesti = FirstLookupValue(LocationOptions).ToString(CultureInfo.InvariantCulture);
            OperationIdLot = "";
            OperationQuantitatValue = 1;
            OperationNouTotalValue = 0;
            OperationMotiu = "";
        }

        private bool ValidateOperation()
        {
            var errors = new List<string>();
            if (OperationPanelMode == StockOperationPanelMode.Entrada)
            {
                ValidateRequiredLookup(OperationIdProducte, ProductOptions, "Producte", errors);
                ValidateRequiredLookup(OperationIdUbicacio, LocationOptions, "Ubicacio", errors);
                ValidateOptionalLot(errors);
            }
            else if (OperationPanelMode == StockOperationPanelMode.Moviment)
            {
                ValidateRequiredLookup(OperationIdProducte, ProductOptions, "Producte", errors);
                ValidateRequiredLookup(OperationIdUbicacioOrigen, LocationOptions, "Ubicacio origen", errors);
                ValidateRequiredLookup(OperationIdUbicacioDesti, LocationOptions, "Ubicacio desti", errors);
                ValidateOptionalLot(errors);
                if (OperationIdUbicacioOrigen == OperationIdUbicacioDesti)
                {
                    errors.Add("Origen i desti han de ser diferents.");
                }
            }
            else if (SelectedRecords.Count != 1)
            {
                errors.Add("Cal seleccionar un unic registre de stock.");
            }

            if (OperationPanelMode == StockOperationPanelMode.Ajust)
            {
                if (OperationNouTotalValue < 0)
                {
                    errors.Add("El nou total no pot ser negatiu.");
                }
            }
            else if (OperationQuantitatValue < 1)
            {
                errors.Add("La quantitat ha de ser superior a 0.");
            }

            ValidationMessage = string.Join("\n", errors);
            return errors.Count == 0;
        }

        private void ValidateOptionalLot(ICollection<string> errors)
        {
            if (!string.IsNullOrWhiteSpace(OperationIdLot) && !LotOptions.Any(option => option.Value == OperationIdLot))
            {
                errors.Add("Lot s'ha de triar del desplegable.");
            }
        }

        private void SetOperationProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (SetProperty(ref storage, value, propertyName) && IsOperationPanelOpen)
            {
                ValidateOperation();
                if (propertyName is nameof(OperationIdUbicacio) or nameof(OperationIdUbicacioDesti))
                {
                    RebuildLocationPicker();
                }
            }
        }

        private async Task LoadLocationPickerRecordsAsync()
        {
            WarehouseLoadResult<LocationRecord> result;
            try
            {
                result = await _locationDataService.LoadAsync();
            }
            catch
            {
                result = await _locationDataService.LoadCachedAsync();
            }

            LocationPickerRecords.Clear();
            foreach (var location in result.Records.OrderBy(record => record.Zona)
                .ThenBy(record => record.Passadis)
                .ThenBy(record => record.BlocEstanteria)
                .ThenBy(record => record.Fila)
                .ThenBy(record => record.Columna))
            {
                LocationPickerRecords.Add(location);
            }

            RebuildLocationPicker();
        }

        private void RebuildLocationPicker()
        {
            var query = LocationPickerSearchText.Trim().ToLowerInvariant();
            var records = string.IsNullOrWhiteSpace(query)
                ? LocationPickerRecords
                : LocationPickerRecords.Where(record =>
                    record.IdText.Contains(query)
                    || record.CodiText.ToLowerInvariant().Contains(query)
                    || record.Zona.ToString(CultureInfo.InvariantCulture).Contains(query)
                    || record.Passadis.ToString(CultureInfo.InvariantCulture).Contains(query));

            LocationPickerZones.Clear();
            foreach (var zoneGroup in records
                .Where(record => !record.IsPendingDelete)
                .GroupBy(record => record.Zona)
                .OrderBy(group => group.Key))
            {
                var zone = new LocationDesignerZone(zoneGroup.Key);
                foreach (var blockGroup in zoneGroup
                    .GroupBy(record => new { record.Passadis, record.BlocEstanteria })
                    .OrderBy(group => group.Key.Passadis)
                    .ThenBy(group => group.Key.BlocEstanteria))
                {
                    var blockRecords = blockGroup.ToList();
                    var block = new LocationDesignerBlock(blockGroup.Key.Passadis, blockGroup.Key.BlocEstanteria);
                    var minFila = blockRecords.Min(record => record.Fila);
                    var maxFila = blockRecords.Max(record => record.Fila);
                    var minColumna = blockRecords.Min(record => record.Columna);
                    var maxColumna = blockRecords.Max(record => record.Columna);

                    for (var fila = minFila; fila <= maxFila; fila++)
                    {
                        var row = new LocationDesignerRow(fila);
                        for (var columna = minColumna; columna <= maxColumna; columna++)
                        {
                            var record = blockRecords.FirstOrDefault(item => item.Fila == fila && item.Columna == columna);
                            var occupancy = GetLocationOccupancy(record?.Id);
                            row.Cells.Add(new LocationDesignerCell(
                                zoneGroup.Key,
                                blockGroup.Key.Passadis,
                                blockGroup.Key.BlocEstanteria,
                                fila,
                                columna,
                                record,
                                occupancy.IsOccupied,
                                IsCurrentLocationSelection(record?.Id),
                                occupancy.Text));
                        }

                        block.Rows.Add(row);
                    }

                    zone.Blocks.Add(block);
                }

                LocationPickerZones.Add(zone);
            }
        }

        private (bool IsOccupied, string Text) GetLocationOccupancy(int? locationId)
        {
            if (locationId is null)
            {
                return (false, "Lliure");
            }

            var stock = Records
                .Where(record => record.IdUbicacio == locationId.Value && !record.IsPendingDelete && record.TotalsEnStock > 0)
                .ToList();
            if (stock.Count == 0)
            {
                return (false, "Lliure");
            }

            var units = stock.Sum(record => record.TotalsEnStock);
            var products = stock.Select(record => record.IdProducte).Distinct().Count();
            return (true, $"{units} u. · {products} prod.");
        }

        private bool IsCurrentLocationSelection(int? locationId)
        {
            if (locationId is null)
            {
                return false;
            }

            var current = OperationPanelMode switch
            {
                StockOperationPanelMode.Entrada => ParseInt(OperationIdUbicacio, 0),
                StockOperationPanelMode.Moviment => ParseInt(OperationIdUbicacioDesti, 0),
                _ => ParseInt(DraftIdUbicacio, 0)
            };

            return current == locationId.Value;
        }
    }

    public sealed class LocationCatalogViewModel : WarehouseCatalogViewModel<LocationRecord>
    {
        private readonly LocationDataService _dataService;
        private double _draftZonaValue = 1;
        private double _draftPassadisValue = 1;
        private double _draftBlocEstanteriaValue = 1;
        private double _draftFilaValue = 1;
        private double _draftColumnaValue = 1;
        private bool _isGeneratorOpen;
        private string _generatorValidationMessage = "";
        private double _generatorZonaValue = 1;
        private double _generatorPassadisFromValue = 1;
        private double _generatorPassadisToValue = 1;
        private double _generatorBlocFromValue = 1;
        private double _generatorBlocToValue = 2;
        private double _generatorFilaFromValue = 1;
        private double _generatorFilaToValue = 5;
        private double _generatorColumnaFromValue = 1;
        private double _generatorColumnaToValue = 10;
        private bool _isGeneratingLocations;
        private string _generationProgressText = "";

        public LocationCatalogViewModel()
            : this(App.CurrentServices.LocationDataService, App.CurrentServices.ShellViewModel)
        {
        }

        public LocationCatalogViewModel(LocationDataService dataService, ShellViewModel shell)
            : base(dataService, shell)
        {
            _dataService = dataService;
        }

        public ObservableCollection<LocationDesignerZone> DesignerZones { get; } = new();

        public double DraftZonaValue { get => _draftZonaValue; set => SetDraftProperty(ref _draftZonaValue, value); }
        public double DraftPassadisValue { get => _draftPassadisValue; set => SetDraftProperty(ref _draftPassadisValue, value); }
        public double DraftBlocEstanteriaValue { get => _draftBlocEstanteriaValue; set => SetDraftProperty(ref _draftBlocEstanteriaValue, value); }
        public double DraftFilaValue { get => _draftFilaValue; set => SetDraftProperty(ref _draftFilaValue, value); }
        public double DraftColumnaValue { get => _draftColumnaValue; set => SetDraftProperty(ref _draftColumnaValue, value); }
        public bool IsGeneratorOpen { get => _isGeneratorOpen; private set => SetProperty(ref _isGeneratorOpen, value); }
        public string GeneratorValidationMessage
        {
            get => _generatorValidationMessage;
            private set
            {
                if (SetProperty(ref _generatorValidationMessage, value))
                {
                    OnPropertyChanged(nameof(HasGeneratorValidationErrors));
                }
            }
        }

        public bool HasGeneratorValidationErrors => !string.IsNullOrWhiteSpace(GeneratorValidationMessage);
        public bool IsGeneratingLocations { get => _isGeneratingLocations; private set => SetProperty(ref _isGeneratingLocations, value); }
        public string GenerationProgressText { get => _generationProgressText; private set => SetProperty(ref _generationProgressText, value); }
        public double GeneratorZonaValue { get => _generatorZonaValue; set => SetGeneratorProperty(ref _generatorZonaValue, value); }
        public double GeneratorPassadisFromValue { get => _generatorPassadisFromValue; set => SetGeneratorProperty(ref _generatorPassadisFromValue, value); }
        public double GeneratorPassadisToValue { get => _generatorPassadisToValue; set => SetGeneratorProperty(ref _generatorPassadisToValue, value); }
        public double GeneratorBlocFromValue { get => _generatorBlocFromValue; set => SetGeneratorProperty(ref _generatorBlocFromValue, value); }
        public double GeneratorBlocToValue { get => _generatorBlocToValue; set => SetGeneratorProperty(ref _generatorBlocToValue, value); }
        public double GeneratorFilaFromValue { get => _generatorFilaFromValue; set => SetGeneratorProperty(ref _generatorFilaFromValue, value); }
        public double GeneratorFilaToValue { get => _generatorFilaToValue; set => SetGeneratorProperty(ref _generatorFilaToValue, value); }
        public double GeneratorColumnaFromValue { get => _generatorColumnaFromValue; set => SetGeneratorProperty(ref _generatorColumnaFromValue, value); }
        public double GeneratorColumnaToValue { get => _generatorColumnaToValue; set => SetGeneratorProperty(ref _generatorColumnaToValue, value); }
        public int GeneratorPreviewCount => GetRangeCount(GeneratorPassadisFromValue, GeneratorPassadisToValue)
            * GetRangeCount(GeneratorBlocFromValue, GeneratorBlocToValue)
            * GetRangeCount(GeneratorFilaFromValue, GeneratorFilaToValue)
            * GetRangeCount(GeneratorColumnaFromValue, GeneratorColumnaToValue);
        public string GeneratorSummary => $"{GeneratorPreviewCount} ubicacions noves com a maxim. Es saltaran les que ja existeixin.";
        public int ZonesCount => Records.Select(record => record.Zona).Distinct().Count();
        public int FreeLikeCount => Records.Count(record => !record.IsPendingDelete);

        protected override string SingularName => "Ubicacio";
        protected override string SingularNameLower => "ubicacio";

        protected override bool MatchesSearch(LocationRecord record, string query)
        {
            return record.IdText.Contains(query)
                || record.CodiText.ToLowerInvariant().Contains(query)
                || record.Zona.ToString(CultureInfo.InvariantCulture).Contains(query)
                || record.Passadis.ToString(CultureInfo.InvariantCulture).Contains(query)
                || record.SyncStateText.ToLowerInvariant().Contains(query);
        }

        protected override LocationRecord CreateEmptyDraft()
        {
            return new LocationRecord { Zona = 1, Passadis = 1, BlocEstanteria = 1, Fila = 1, Columna = 1 };
        }

        protected override void LoadDraft(LocationRecord record)
        {
            ValidationMessage = "";
            DraftZonaValue = record.Zona;
            DraftPassadisValue = record.Passadis;
            DraftBlocEstanteriaValue = record.BlocEstanteria;
            DraftFilaValue = record.Fila;
            DraftColumnaValue = record.Columna;
        }

        protected override bool ValidateDraft()
        {
            var errors = new List<string>();
            ValidatePositive(DraftZonaValue, "Zona", errors);
            ValidatePositive(DraftPassadisValue, "Passadis", errors);
            ValidatePositive(DraftBlocEstanteriaValue, "Bloc estanteria", errors);
            ValidatePositive(DraftFilaValue, "Fila", errors);
            ValidatePositive(DraftColumnaValue, "Columna", errors);
            ValidationMessage = string.Join("\n", errors);
            return errors.Count == 0;
        }

        protected override LocationRecord CreateFromDraft(int id)
        {
            return new LocationRecord
            {
                Id = id,
                Zona = Math.Max(1, FromNumberValue(DraftZonaValue, 1)),
                Passadis = Math.Max(1, FromNumberValue(DraftPassadisValue, 1)),
                BlocEstanteria = Math.Max(1, FromNumberValue(DraftBlocEstanteriaValue, 1)),
                Fila = Math.Max(1, FromNumberValue(DraftFilaValue, 1)),
                Columna = Math.Max(1, FromNumberValue(DraftColumnaValue, 1))
            };
        }

        protected override void SaveDraftInto(LocationRecord record)
        {
            var updated = CreateFromDraft(record.Id);
            record.Zona = updated.Zona;
            record.Passadis = updated.Passadis;
            record.BlocEstanteria = updated.BlocEstanteria;
            record.Fila = updated.Fila;
            record.Columna = updated.Columna;
        }

        public void SetDesignerView()
        {
            SetViewMode(WarehouseCatalogViewMode.Designer);
            RebuildDesigner();
        }

        public void OpenGenerator()
        {
            CancelPanel();
            GeneratorValidationMessage = "";
            IsGeneratorOpen = true;
            StatusText = "Defineix rangs per generar ubicacions de forma massiva.";
            ValidateGenerator();
        }

        public void CancelGenerator()
        {
            IsGeneratorOpen = false;
            GeneratorValidationMessage = "";
            StatusText = "Generacio cancelada.";
        }

        public void ActivateDesignerCell(LocationDesignerCell cell)
        {
            if (cell.Record is null)
            {
                OpenCreatePanel(CreateFromCell(cell), $"Nova ubicacio a {cell.CoordinatesText}.");
                return;
            }

            SelectSingle(cell.Record);
        }

        public void EditDesignerCell(LocationDesignerCell cell)
        {
            if (cell.Record is null)
            {
                OpenCreatePanel(CreateFromCell(cell), $"Nova ubicacio a {cell.CoordinatesText}.");
                return;
            }

            SelectSingle(cell.Record);
            StartEditSelected();
        }

        public async Task<int> GenerateLocationsAsync()
        {
            if (IsGeneratingLocations)
            {
                StatusText = "La generacio d'ubicacions ja esta en curs.";
                return 0;
            }

            if (!ValidateGenerator())
            {
                StatusText = "Revisa els rangs del generador abans de continuar.";
                return 0;
            }

            IsGeneratorOpen = false;
            IsGeneratingLocations = true;
            GenerationProgressText = "Preparant ubicacions...";
            StatusText = "Generant ubicacions en segon pla...";

            try
            {
                var generationRequest = CreateGenerationRequest();
                var existingKeys = Records
                    .Where(record => !record.IsPendingDelete)
                    .Select(LocationKey)
                    .ToHashSet(StringComparer.Ordinal);

                var drafts = await Task.Run(() => BuildGeneratedLocations(generationRequest, existingKeys));
                var skipped = GeneratorPreviewCount - drafts.Count;

                if (drafts.Count == 0)
                {
                    StatusText = "No hi ha ubicacions noves per generar; totes ja existeixen.";
                    return 0;
                }

                GenerationProgressText = $"Enviant {drafts.Count} ubicacions a l'API o cua offline...";
                var result = await _dataService.CreateManyAsync(drafts, Records.ToList());
                ApplyMutation(result);

                StatusText = skipped == 0
                    ? $"{drafts.Count} ubicacions generades."
                    : $"{drafts.Count} ubicacions generades; {skipped} ja existien i s'han saltat.";
                RebuildDesigner();
                return drafts.Count;
            }
            finally
            {
                GenerationProgressText = "";
                IsGeneratingLocations = false;
            }
        }

        protected override void RaiseSpecificMetrics()
        {
            OnPropertyChanged(nameof(ZonesCount));
            OnPropertyChanged(nameof(FreeLikeCount));
        }

        protected override void OnRecordsChanged() => RebuildDesigner();
        protected override void OnFilterChanged() => RebuildDesigner();
        protected override void OnSelectionChanged() => RebuildDesigner();

        private void SetGeneratorProperty(ref double storage, double value, [CallerMemberName] string? propertyName = null)
        {
            if (SetProperty(ref storage, value, propertyName))
            {
                ValidateGenerator();
                OnPropertyChanged(nameof(GeneratorPreviewCount));
                OnPropertyChanged(nameof(GeneratorSummary));
            }
        }

        private bool ValidateGenerator()
        {
            var errors = new List<string>();
            ValidatePositive(GeneratorZonaValue, "Zona", errors);
            ValidateRange(GeneratorPassadisFromValue, GeneratorPassadisToValue, "Passadis", errors);
            ValidateRange(GeneratorBlocFromValue, GeneratorBlocToValue, "Bloc estanteria", errors);
            ValidateRange(GeneratorFilaFromValue, GeneratorFilaToValue, "Fila", errors);
            ValidateRange(GeneratorColumnaFromValue, GeneratorColumnaToValue, "Columna", errors);

            if (GeneratorPreviewCount > 1000)
            {
                errors.Add("Limita la generacio a 1000 ubicacions per operacio.");
            }

            GeneratorValidationMessage = string.Join("\n", errors);
            return errors.Count == 0;
        }

        private void RebuildDesigner()
        {
            DesignerZones.Clear();
            foreach (var zoneGroup in FilteredRecords
                .Where(record => !record.IsPendingDelete)
                .GroupBy(record => record.Zona)
                .OrderBy(group => group.Key))
            {
                var zone = new LocationDesignerZone(zoneGroup.Key);
                foreach (var blockGroup in zoneGroup
                    .GroupBy(record => new { record.Passadis, record.BlocEstanteria })
                    .OrderBy(group => group.Key.Passadis)
                    .ThenBy(group => group.Key.BlocEstanteria))
                {
                    var records = blockGroup.ToList();
                    var block = new LocationDesignerBlock(blockGroup.Key.Passadis, blockGroup.Key.BlocEstanteria);
                    var minFila = records.Min(record => record.Fila);
                    var maxFila = records.Max(record => record.Fila);
                    var minColumna = records.Min(record => record.Columna);
                    var maxColumna = records.Max(record => record.Columna);

                    for (var fila = minFila; fila <= maxFila; fila++)
                    {
                        var row = new LocationDesignerRow(fila);
                        for (var columna = minColumna; columna <= maxColumna; columna++)
                        {
                            var record = records.FirstOrDefault(item => item.Fila == fila && item.Columna == columna);
                            row.Cells.Add(new LocationDesignerCell(zoneGroup.Key, blockGroup.Key.Passadis, blockGroup.Key.BlocEstanteria, fila, columna, record));
                        }

                        block.Rows.Add(row);
                    }

                    zone.Blocks.Add(block);
                }

                DesignerZones.Add(zone);
            }
        }

        private static LocationRecord CreateFromCell(LocationDesignerCell cell)
        {
            return new LocationRecord
            {
                Zona = cell.Zona,
                Passadis = cell.Passadis,
                BlocEstanteria = cell.BlocEstanteria,
                Fila = cell.Fila,
                Columna = cell.Columna
            };
        }

        private static string LocationKey(LocationRecord record)
        {
            return $"{record.Zona}:{record.Passadis}:{record.BlocEstanteria}:{record.Fila}:{record.Columna}";
        }

        private LocationGenerationRequest CreateGenerationRequest()
        {
            return new LocationGenerationRequest(
                Math.Max(1, FromNumberValue(GeneratorZonaValue, 1)),
                GetRange(GeneratorPassadisFromValue, GeneratorPassadisToValue),
                GetRange(GeneratorBlocFromValue, GeneratorBlocToValue),
                GetRange(GeneratorFilaFromValue, GeneratorFilaToValue),
                GetRange(GeneratorColumnaFromValue, GeneratorColumnaToValue));
        }

        private static List<LocationRecord> BuildGeneratedLocations(LocationGenerationRequest request, HashSet<string> existingKeys)
        {
            var drafts = new List<LocationRecord>();
            foreach (var passadis in request.Passadissos)
            {
                foreach (var bloc in request.Blocs)
                {
                    foreach (var fila in request.Files)
                    {
                        foreach (var columna in request.Columnes)
                        {
                            var draft = new LocationRecord
                            {
                                Zona = request.Zona,
                                Passadis = passadis,
                                BlocEstanteria = bloc,
                                Fila = fila,
                                Columna = columna
                            };

                            if (!existingKeys.Add(LocationKey(draft)))
                            {
                                continue;
                            }

                            drafts.Add(draft);
                        }
                    }
                }
            }

            return drafts;
        }

        private static IReadOnlyList<int> GetRange(double fromValue, double toValue)
        {
            var from = Math.Max(1, FromNumberValue(fromValue, 1));
            var to = Math.Max(1, FromNumberValue(toValue, from));
            if (to < from)
            {
                (from, to) = (to, from);
            }

            return Enumerable.Range(from, to - from + 1).ToList();
        }

        private static int GetRangeCount(double fromValue, double toValue)
        {
            return GetRange(fromValue, toValue).Count;
        }

        private static void ValidateRange(double fromValue, double toValue, string label, ICollection<string> errors)
        {
            ValidatePositive(fromValue, $"{label} inicial", errors);
            ValidatePositive(toValue, $"{label} final", errors);
        }

        private static void ValidatePositive(double value, string label, ICollection<string> errors)
        {
            if (double.IsNaN(value) || value < 1)
            {
                errors.Add($"{label} ha de ser igual o superior a 1.");
            }
        }

        private sealed record LocationGenerationRequest(
            int Zona,
            IReadOnlyList<int> Passadissos,
            IReadOnlyList<int> Blocs,
            IReadOnlyList<int> Files,
            IReadOnlyList<int> Columnes);
    }

    public sealed class LocationDesignerZone
    {
        public LocationDesignerZone(int zona)
        {
            Zona = zona;
            Title = $"Zona {zona}";
        }

        public int Zona { get; }
        public string Title { get; }
        public ObservableCollection<LocationDesignerBlock> Blocks { get; } = new();
    }

    public sealed class LocationDesignerBlock
    {
        public LocationDesignerBlock(int passadis, int blocEstanteria)
        {
            Passadis = passadis;
            BlocEstanteria = blocEstanteria;
            Title = $"Passadis {passadis} · Bloc {blocEstanteria}";
        }

        public int Passadis { get; }
        public int BlocEstanteria { get; }
        public string Title { get; }
        public ObservableCollection<LocationDesignerRow> Rows { get; } = new();
    }

    public sealed class LocationDesignerRow
    {
        public LocationDesignerRow(int fila)
        {
            Fila = fila;
            Title = $"Fila {fila}";
        }

        public int Fila { get; }
        public string Title { get; }
        public ObservableCollection<LocationDesignerCell> Cells { get; } = new();
    }

    public sealed class LocationDesignerCell
    {
        public LocationDesignerCell(
            int zona,
            int passadis,
            int blocEstanteria,
            int fila,
            int columna,
            LocationRecord? record,
            bool isOccupied = false,
            bool isCurrentSelection = false,
            string occupancyText = "")
        {
            Zona = zona;
            Passadis = passadis;
            BlocEstanteria = blocEstanteria;
            Fila = fila;
            Columna = columna;
            Record = record;
            IsOccupied = isOccupied;
            IsCurrentSelection = isCurrentSelection;
            OccupancyText = string.IsNullOrWhiteSpace(occupancyText)
                ? isOccupied ? "Ocupada" : "Lliure"
                : occupancyText;
        }

        public int Zona { get; }
        public int Passadis { get; }
        public int BlocEstanteria { get; }
        public int Fila { get; }
        public int Columna { get; }
        public LocationRecord? Record { get; }
        public bool HasLocation => Record is not null;
        public bool IsSelected => Record?.IsSelected == true;
        public bool IsPending => Record?.IsPending == true;
        public bool IsOccupied { get; }
        public bool IsCurrentSelection { get; }
        public string OccupancyText { get; }
        public string CodeText => Record?.CodiText ?? CoordinatesText;
        public string CoordinatesText => $"Z{Zona}-P{Passadis}-B{BlocEstanteria}-F{Fila}-C{Columna}";
        public string StateText => IsOccupied ? "Ocupada" : Record?.SyncStateText ?? "Lliure";
        public string CellBrushKey => HasLocation ? "AppAccentBrush" : "AppSurfaceAltBrush";
    }

    public sealed class SupplierLotCatalogViewModel : WarehouseCatalogViewModel<SupplierLotRecord>
    {
        private readonly LookupDataService _lookups;
        private string _draftIdProveidor = "";
        private string _draftIdProducte = "";
        private double _draftQuantitatRebudaValue;
        private DateTimeOffset? _draftDataDemanatDate;
        private DateTimeOffset _draftDataRebutDate = DateTimeOffset.Now;
        private DateTimeOffset _draftDataCaducitatDate = DateTimeOffset.Now.AddMonths(6);

        public SupplierLotCatalogViewModel()
            : this(App.CurrentServices.SupplierLotDataService, App.CurrentServices.LookupDataService, App.CurrentServices.ShellViewModel)
        {
        }

        public SupplierLotCatalogViewModel(SupplierLotDataService dataService, LookupDataService lookups, ShellViewModel shell)
            : base(dataService, shell)
        {
            _lookups = lookups;
        }

        public ObservableCollection<LookupOption> SupplierOptions { get; } = new();
        public ObservableCollection<LookupOption> ProductOptions { get; } = new();

        public string DraftIdProveidor { get => _draftIdProveidor; set => SetDraftProperty(ref _draftIdProveidor, value); }
        public string DraftIdProducte { get => _draftIdProducte; set => SetDraftProperty(ref _draftIdProducte, value); }
        public double DraftQuantitatRebudaValue { get => _draftQuantitatRebudaValue; set => SetDraftProperty(ref _draftQuantitatRebudaValue, value); }
        public DateTimeOffset? DraftDataDemanatDate { get => _draftDataDemanatDate; set => SetDraftProperty(ref _draftDataDemanatDate, value); }
        public DateTimeOffset DraftDataRebutDate { get => _draftDataRebutDate; set => SetDraftProperty(ref _draftDataRebutDate, value); }
        public DateTimeOffset DraftDataCaducitatDate { get => _draftDataCaducitatDate; set => SetDraftProperty(ref _draftDataCaducitatDate, value); }
        public int TotalQuantity => Records.Sum(record => record.QuantitatRebuda);
        public int ExpiringCount => Records.Count(record => record.DataCaducitat <= DateOnly.FromDateTime(DateTime.Today.AddDays(30)));

        protected override string SingularName => "Lot";
        protected override string SingularNameLower => "lot";

        protected override async Task LoadLookupsAsync()
        {
            await LoadLookupCollectionAsync(SupplierOptions, await _lookups.GetSuppliersAsync());
            await LoadLookupCollectionAsync(ProductOptions, await _lookups.GetProductsAsync());
        }

        protected override void ApplyLookups(SupplierLotRecord record)
        {
            record.ProveidorText = LookupText(SupplierOptions, record.IdProveidor, $"Proveidor {record.IdProveidor}");
            record.ProducteText = LookupText(ProductOptions, record.IdProducte, $"Producte {record.IdProducte}");
        }

        protected override bool MatchesSearch(SupplierLotRecord record, string query)
        {
            return record.IdText.Contains(query)
                || record.ProveidorText.ToLowerInvariant().Contains(query)
                || record.ProducteText.ToLowerInvariant().Contains(query)
                || record.DataCaducitatText.Contains(query)
                || record.SyncStateText.ToLowerInvariant().Contains(query);
        }

        protected override SupplierLotRecord CreateEmptyDraft()
        {
            return new SupplierLotRecord
            {
                IdProveidor = FirstLookupValue(SupplierOptions),
                IdProducte = FirstLookupValue(ProductOptions),
                QuantitatRebuda = 1,
                DataRebut = DateOnly.FromDateTime(DateTime.Today),
                DataCaducitat = DateOnly.FromDateTime(DateTime.Today.AddMonths(6))
            };
        }

        protected override void LoadDraft(SupplierLotRecord record)
        {
            ValidationMessage = "";
            DraftIdProveidor = record.IdProveidor.ToString(CultureInfo.InvariantCulture);
            DraftIdProducte = record.IdProducte.ToString(CultureInfo.InvariantCulture);
            DraftQuantitatRebudaValue = record.QuantitatRebuda;
            DraftDataDemanatDate = record.DataDemanat is null ? null : ToDateTimeOffset(record.DataDemanat.Value);
            DraftDataRebutDate = ToDateTimeOffset(record.DataRebut);
            DraftDataCaducitatDate = ToDateTimeOffset(record.DataCaducitat);
        }

        protected override bool ValidateDraft()
        {
            var errors = new List<string>();
            ValidateRequiredLookup(DraftIdProveidor, SupplierOptions, "Proveidor", errors);
            ValidateRequiredLookup(DraftIdProducte, ProductOptions, "Producte", errors);
            if (DraftQuantitatRebudaValue < 1)
            {
                errors.Add("Quantitat rebuda ha de ser igual o superior a 1.");
            }

            if (DraftDataCaducitatDate.Date < DraftDataRebutDate.Date)
            {
                errors.Add("Data de caducitat no pot ser anterior a la data de recepcio.");
            }

            ValidationMessage = string.Join("\n", errors);
            return errors.Count == 0;
        }

        protected override SupplierLotRecord CreateFromDraft(int id)
        {
            return new SupplierLotRecord
            {
                Id = id,
                IdProveidor = ParseInt(DraftIdProveidor, FirstLookupValue(SupplierOptions)),
                IdProducte = ParseInt(DraftIdProducte, FirstLookupValue(ProductOptions)),
                QuantitatRebuda = Math.Max(1, FromNumberValue(DraftQuantitatRebudaValue, 1)),
                DataDemanat = DraftDataDemanatDate is null ? null : DateOnly.FromDateTime(DraftDataDemanatDate.Value.DateTime),
                DataRebut = DateOnly.FromDateTime(DraftDataRebutDate.DateTime),
                DataCaducitat = DateOnly.FromDateTime(DraftDataCaducitatDate.DateTime)
            };
        }

        protected override void SaveDraftInto(SupplierLotRecord record)
        {
            var updated = CreateFromDraft(record.Id);
            record.IdProveidor = updated.IdProveidor;
            record.IdProducte = updated.IdProducte;
            record.QuantitatRebuda = updated.QuantitatRebuda;
            record.DataDemanat = updated.DataDemanat;
            record.DataRebut = updated.DataRebut;
            record.DataCaducitat = updated.DataCaducitat;
            ApplyLookups(record);
        }

        protected override void RaiseSpecificMetrics()
        {
            OnPropertyChanged(nameof(TotalQuantity));
            OnPropertyChanged(nameof(ExpiringCount));
        }

        private static DateTimeOffset ToDateTimeOffset(DateOnly date)
        {
            return new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue));
        }
    }

    internal static class WarehouseViewModelHelpers
    {
        public static async Task LoadLookupCollectionAsync(
            ObservableCollection<LookupOption> target,
            IReadOnlyList<LookupOption> source)
        {
            target.Clear();
            foreach (var item in source)
            {
                target.Add(item);
            }

            await Task.CompletedTask;
        }

        public static int FirstLookupValue(IEnumerable<LookupOption> options)
        {
            return options.FirstOrDefault(option => option.Id.HasValue)?.Id ?? 1;
        }

        public static int ParseInt(string value, int fallback)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
        }

        public static string LookupText(IEnumerable<LookupOption> options, int id, string fallback)
        {
            return options.FirstOrDefault(option => option.Id == id)?.DisplayText ?? fallback;
        }
    }
}
