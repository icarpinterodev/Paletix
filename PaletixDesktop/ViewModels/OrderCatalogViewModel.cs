using PaletixDesktop.Models;
using PaletixDesktop.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace PaletixDesktop.ViewModels
{
    public enum OrderPanelMode
    {
        None,
        Create,
        Edit
    }

    public sealed class OrderCatalogViewModel : ViewModelBase
    {
        private readonly OrderDataService _dataService;
        private readonly LookupDataService _lookups;
        private readonly ProductDataService _productDataService;
        private readonly ShellViewModel _shell;
        private string _searchText = "";
        private string _statusText = "Carregant comandes...";
        private OrderPanelMode _panelMode;
        private OrderRecord? _draft;
        private string _validationMessage = "";
        private string _productPickerSearch = "";
        private bool _isProductPickerOpen;
        private int? _draftDefaultLocationId;
        private DateTimeOffset _draftDataPrevistaEntregaDate = DateTimeOffset.Now;
        private DateTimeOffset _draftDataEntregatDate = DateTimeOffset.Now;
        private bool _draftHasDataEntregat;

        public OrderCatalogViewModel()
            : this(App.CurrentServices.OrderDataService, App.CurrentServices.LookupDataService, App.CurrentServices.ProductDataService, App.CurrentServices.ShellViewModel)
        {
        }

        public OrderCatalogViewModel(OrderDataService dataService, LookupDataService lookups, ProductDataService productDataService, ShellViewModel shell)
        {
            _dataService = dataService;
            _lookups = lookups;
            _productDataService = productDataService;
            _shell = shell;
        }

        public ObservableCollection<OrderRecord> Orders { get; } = new();
        public ObservableCollection<OrderRecord> FilteredOrders { get; } = new();
        public ObservableCollection<OrderRecord> SelectedOrders { get; } = new();
        public ObservableCollection<OrderLineRecord> DraftLines { get; } = new();
        public ObservableCollection<LookupOption> ClientOptions { get; } = new();
        public ObservableCollection<LookupOption> UserOptions { get; } = new();
        public ObservableCollection<LookupOption> VehicleOptions { get; } = new();
        public ObservableCollection<LookupOption> StateOptions { get; } = new();
        public ObservableCollection<LookupOption> LocationOptions { get; } = new();
        public ObservableCollection<OrderProductPickerItem> ProductPickerItems { get; } = new();
        public List<OrderProductPickerItem> AllProductPickerItems { get; } = new();

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

        public string StatusText
        {
            get => _statusText;
            private set => SetProperty(ref _statusText, value);
        }

        public OrderPanelMode PanelMode
        {
            get => _panelMode;
            private set
            {
                if (SetProperty(ref _panelMode, value))
                {
                    OnPropertyChanged(nameof(IsPanelOpen));
                    OnPropertyChanged(nameof(PanelTitle));
                    OnPropertyChanged(nameof(PanelSubtitle));
                }
            }
        }

        public bool IsPanelOpen => PanelMode != OrderPanelMode.None;
        public string PanelTitle => PanelMode == OrderPanelMode.Create ? "Nova comanda" : "Editar comanda";
        public string PanelSubtitle => Draft is null ? "" : PanelMode == OrderPanelMode.Create ? "Capcalera i linies de la nova comanda" : $"Comanda #{Draft.Id}";

        public OrderRecord? Draft
        {
            get => _draft;
            private set
            {
                if (SetProperty(ref _draft, value))
                {
                    RaiseDraftProperties();
                }
            }
        }

        public string ValidationMessage
        {
            get => _validationMessage;
            private set
            {
                if (SetProperty(ref _validationMessage, value))
                {
                    OnPropertyChanged(nameof(HasValidationErrors));
                }
            }
        }

        public bool HasValidationErrors => !string.IsNullOrWhiteSpace(ValidationMessage);

        public string ProductPickerSearch
        {
            get => _productPickerSearch;
            set
            {
                if (SetProperty(ref _productPickerSearch, value))
                {
                    RefreshProductPicker();
                }
            }
        }

        public bool IsProductPickerOpen
        {
            get => _isProductPickerOpen;
            set => SetProperty(ref _isProductPickerOpen, value);
        }

        public int TotalOrders => Orders.Count;
        public int PendingOrders => Orders.Count(order => order.IsPending);
        public int OpenOrders => Orders.Count(order => !order.IsPendingDelete);
        public int SelectedCount => SelectedOrders.Count;
        public string SelectedCountText => $"{SelectedCount} seleccionada(es)";

        public int? DraftIdClient { get => Draft?.IdClient; set => SetDraftValue(order => order.IdClient = value ?? 0); }
        public int? DraftIdChofer { get => Draft?.IdChofer; set => SetDraftValue(order => order.IdChofer = value ?? 0); }
        public int? DraftIdPreparador { get => Draft?.IdPreparador; set => SetDraftValue(order => order.IdPreparador = value ?? 0); }
        public int? DraftIdVehicleTransportista { get => Draft?.IdVehicleTransportista; set => SetDraftValue(order => order.IdVehicleTransportista = value ?? 0); }
        public int? DraftIdEstat { get => Draft?.IdEstat; set => SetDraftValue(order => order.IdEstat = value ?? 0); }
        public string? DraftNotes { get => Draft?.Notes; set => SetDraftValue(order => order.Notes = value); }
        public string? DraftPoblacioEntregaAlternativa { get => Draft?.PoblacioEntregaAlternativa; set => SetDraftValue(order => order.PoblacioEntregaAlternativa = value); }
        public string? DraftAdrecaEntregaAlternativa { get => Draft?.AdrecaEntregaAlternativa; set => SetDraftValue(order => order.AdrecaEntregaAlternativa = value); }

        public int? DraftDefaultLocationId
        {
            get => _draftDefaultLocationId;
            set
            {
                if (SetProperty(ref _draftDefaultLocationId, value))
                {
                    SyncProductPickerSelection();
                }
            }
        }

        public DateTimeOffset DraftDataPrevistaEntregaDate
        {
            get => _draftDataPrevistaEntregaDate;
            set
            {
                if (SetProperty(ref _draftDataPrevistaEntregaDate, value))
                {
                    SetDraftValue(order => order.DataPrevistaEntrega = DateOnly.FromDateTime(value.DateTime));
                }
            }
        }

        public bool DraftHasDataEntregat
        {
            get => _draftHasDataEntregat;
            set
            {
                if (SetProperty(ref _draftHasDataEntregat, value))
                {
                    SetDraftValue(order => order.DataEntregat = value ? DateOnly.FromDateTime(DraftDataEntregatDate.DateTime) : null);
                }
            }
        }

        public DateTimeOffset DraftDataEntregatDate
        {
            get => _draftDataEntregatDate;
            set
            {
                if (SetProperty(ref _draftDataEntregatDate, value) && DraftHasDataEntregat)
                {
                    SetDraftValue(order => order.DataEntregat = DateOnly.FromDateTime(value.DateTime));
                }
            }
        }

        public async Task LoadAsync()
        {
            IsBusy = true;
            ErrorMessage = null;

            try
            {
                await LoadLookupsAsync();
                var result = await _dataService.LoadAsync();
                ApplyRecords(result.Records);
                StatusText = result.Message;
                await _shell.RefreshSyncStatusAsync(result.IsOnline, result.Message);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                StatusText = $"No s'han pogut carregar les comandes: {ex.Message}";
                await _shell.RefreshSyncStatusAsync(false, "Mode offline");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void SetSelection(IEnumerable<OrderRecord> orders)
        {
            foreach (var order in Orders)
            {
                order.IsSelected = false;
            }

            SelectedOrders.Clear();
            foreach (var order in orders.Distinct())
            {
                order.IsSelected = true;
                SelectedOrders.Add(order);
            }

            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(SelectedCountText));
        }

        public void OpenCreate()
        {
            var today = DateOnly.FromDateTime(DateTime.Now.Date);
            var draft = new OrderRecord
            {
                Id = 0,
                IdClient = FirstId(ClientOptions),
                IdChofer = FirstId(UserOptions),
                IdPreparador = FirstId(UserOptions),
                IdVehicleTransportista = FirstId(VehicleOptions),
                IdEstat = FirstId(StateOptions),
                DataPrevistaEntrega = today,
                SyncState = OrderSyncState.Synced,
                SyncMessage = "Nova comanda"
            };

            OpenPanel(OrderPanelMode.Create, draft);
            StatusText = "Introdueix la capcalera i afegeix linies de producte.";
        }

        public void OpenEditSelected()
        {
            var selected = SelectedOrders.FirstOrDefault();
            if (selected is null)
            {
                StatusText = "Selecciona una comanda per editar.";
                return;
            }

            if (selected.IsPendingDelete)
            {
                StatusText = "No es pot editar una comanda marcada per eliminar.";
                return;
            }

            OpenPanel(OrderPanelMode.Edit, Clone(selected));
            StatusText = $"Editant comanda #{selected.Id}.";
        }

        public void ClosePanel()
        {
            PanelMode = OrderPanelMode.None;
            Draft = null;
            DraftLines.Clear();
            IsProductPickerOpen = false;
            ValidationMessage = "";
        }

        public async Task SaveAsync()
        {
            if (Draft is null)
            {
                return;
            }

            Draft.Lines = DraftLines.Select(CloneLine).ToList();
            if (!ValidateDraft(Draft))
            {
                return;
            }

            IsBusy = true;
            try
            {
                var result = PanelMode == OrderPanelMode.Create
                    ? await _dataService.CreateAsync(Draft, Orders.ToList())
                    : await _dataService.UpdateAsync(Draft, Orders.ToList());
                ApplyRecords(result.Records);
                await _shell.RefreshSyncStatusAsync(message: result.PendingCount == 0 ? "Sincronitzat" : "Canvis pendents");
                StatusText = result.Message;
                ClosePanel();
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeleteSelectedAsync()
        {
            if (SelectedOrders.Count == 0)
            {
                StatusText = "Selecciona una o mes comandes per eliminar.";
                return;
            }

            IsBusy = true;
            try
            {
                OrderMutationResult? last = null;
                foreach (var order in SelectedOrders.ToList())
                {
                    last = await _dataService.DeleteAsync(order, (last?.Records ?? Orders).ToList());
                }

                if (last is not null)
                {
                    ApplyRecords(last.Records);
                    await _shell.RefreshSyncStatusAsync(message: last.PendingCount == 0 ? "Sincronitzat" : "Canvis pendents");
                    StatusText = last.Message;
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void OpenProductPicker()
        {
            if (Draft is null)
            {
                StatusText = "Obre una comanda abans d'afegir productes.";
                return;
            }

            ProductPickerSearch = "";
            SyncProductPickerSelection();
            IsProductPickerOpen = true;
        }

        public void ToggleProductFromPicker(OrderProductPickerItem item, bool isSelected)
        {
            if (Draft is null)
            {
                return;
            }

            if (!DraftDefaultLocationId.HasValue || DraftDefaultLocationId <= 0)
            {
                ValidationMessage = "Selecciona una ubicacio predeterminada abans d'afegir productes.";
                item.IsSelected = false;
                return;
            }

            if (!isSelected)
            {
                var existingLine = DraftLines.FirstOrDefault(line => line.IdProducte == item.Id && line.IdUbicacio == DraftDefaultLocationId);
                if (existingLine is not null)
                {
                    DraftLines.Remove(existingLine);
                }

                StatusText = $"{item.Label} tret de la comanda.";
                return;
            }

            var caixes = Math.Max(1, (int)Math.Round(item.CaixesValue));
            var palets = Math.Max(0, (int)Math.Round(item.PaletsValue));
            var existing = DraftLines.FirstOrDefault(line => line.IdProducte == item.Id && line.IdUbicacio == DraftDefaultLocationId);
            if (existing is not null)
            {
                existing.Caixes = caixes;
                existing.Palets = palets == 0 ? null : palets;
            }
            else
            {
                DraftLines.Add(new OrderLineRecord
                {
                    Id = NextDraftLineId(),
                    IdProducte = item.Id,
                    ProducteText = item.Label,
                    IdUbicacio = DraftDefaultLocationId,
                    Palets = palets == 0 ? null : palets,
                    Caixes = caixes
                });
            }

            ValidationMessage = "";
            StatusText = $"{item.Label} afegit a la comanda.";
        }

        public void RemoveDraftLine(OrderLineRecord? line)
        {
            if (line is null)
            {
                StatusText = "Selecciona una linia per treure-la.";
                return;
            }

            DraftLines.Remove(line);
            var matchingPicker = AllProductPickerItems.FirstOrDefault(item => item.Id == line.IdProducte);
            if (matchingPicker is not null)
            {
                matchingPicker.IsSelected = false;
            }

            StatusText = $"{line.ProducteText} tret de la comanda.";
        }

        private async Task LoadLookupsAsync()
        {
            await ReplaceAsync(ClientOptions, await _lookups.GetClientsAsync());
            await ReplaceAsync(UserOptions, await _lookups.GetUsersAsync());
            await ReplaceAsync(VehicleOptions, await _lookups.GetVehiclesAsync());
            await ReplaceAsync(StateOptions, await _lookups.GetStatesAsync());
            await ReplaceAsync(LocationOptions, await _lookups.GetLocationsAsync());

            IReadOnlyList<ProductRecord>? productRecords = null;
            try
            {
                productRecords = (await _productDataService.LoadAsync()).Products;
            }
            catch
            {
                productRecords = null;
            }

            var products = await _lookups.GetProductsAsync();
            var images = productRecords?
                .ToDictionary(product => product.Id, product => product.ImatgeUrl)
                ?? new Dictionary<int, string?>();
            AllProductPickerItems.Clear();
            AllProductPickerItems.AddRange(products.Where(item => item.Id.HasValue).Select(item => new OrderProductPickerItem
            {
                Id = item.Id!.Value,
                Label = item.Label,
                ImageUrl = images.TryGetValue(item.Id!.Value, out var imageUrl) ? imageUrl : null
            }));
            RefreshProductPicker();
        }

        private static Task ReplaceAsync<T>(ObservableCollection<T> target, IReadOnlyList<T> items)
        {
            target.Clear();
            foreach (var item in items)
            {
                target.Add(item);
            }

            return Task.CompletedTask;
        }

        private void OpenPanel(OrderPanelMode mode, OrderRecord draft)
        {
            Draft = draft;
            DraftLines.Clear();
            foreach (var line in draft.Lines.Select(CloneLine))
            {
                DraftLines.Add(line);
            }

            DraftDataPrevistaEntregaDate = new DateTimeOffset(draft.DataPrevistaEntrega.ToDateTime(TimeOnly.MinValue));
            DraftHasDataEntregat = draft.DataEntregat.HasValue;
            DraftDataEntregatDate = new DateTimeOffset((draft.DataEntregat ?? draft.DataPrevistaEntrega).ToDateTime(TimeOnly.MinValue));
            DraftDefaultLocationId = LocationOptions.FirstOrDefault()?.Id;
            ValidationMessage = "";
            PanelMode = mode;
            SyncProductPickerSelection();
            RaiseDraftProperties();
        }

        private bool ValidateDraft(OrderRecord draft)
        {
            if (draft.IdClient <= 0 || draft.IdChofer <= 0 || draft.IdPreparador <= 0 || draft.IdVehicleTransportista <= 0 || draft.IdEstat <= 0)
            {
                ValidationMessage = "Client, xofer, preparador, vehicle i estat son obligatoris.";
                return false;
            }

            if (DraftLines.Count == 0)
            {
                ValidationMessage = "La comanda ha de tenir com a minim una linia.";
                return false;
            }

            var invalidLine = DraftLines.FirstOrDefault(line => line.IdProducte <= 0 || !line.IdUbicacio.HasValue || line.IdUbicacio <= 0 || line.Caixes <= 0);
            if (invalidLine is not null)
            {
                ValidationMessage = "Cada linia ha de tenir producte, ubicacio i caixes superiors a 0.";
                return false;
            }

            ValidationMessage = "";
            return true;
        }

        private void ApplyRecords(IReadOnlyList<OrderRecord> records)
        {
            Orders.Clear();
            foreach (var record in records.OrderByDescending(order => order.Id))
            {
                Orders.Add(record);
            }

            RefreshFilter();
            SetSelection(Array.Empty<OrderRecord>());
            RaiseMetrics();
        }

        private void RefreshFilter()
        {
            var query = SearchText.Trim().ToLowerInvariant();
            var filtered = string.IsNullOrWhiteSpace(query)
                ? Orders
                : Orders.Where(order =>
                    order.IdText.Contains(query)
                    || order.ClientText.ToLowerInvariant().Contains(query)
                    || order.EstatText.ToLowerInvariant().Contains(query)
                    || order.VehicleText.ToLowerInvariant().Contains(query)
                    || order.SyncStateText.ToLowerInvariant().Contains(query));

            var selected = SelectedOrders.ToList();
            FilteredOrders.Clear();
            foreach (var order in filtered)
            {
                FilteredOrders.Add(order);
            }

            SetSelection(selected.Where(FilteredOrders.Contains).ToList());
        }

        private void RefreshProductPicker()
        {
            var query = ProductPickerSearch.Trim().ToLowerInvariant();
            var products = string.IsNullOrWhiteSpace(query)
                ? AllProductPickerItems
                : AllProductPickerItems.Where(item => item.Id.ToString(CultureInfo.InvariantCulture).Contains(query) || item.Label.ToLowerInvariant().Contains(query));

            ProductPickerItems.Clear();
            foreach (var item in products.Take(80))
            {
                ProductPickerItems.Add(item);
            }
        }

        private void SyncProductPickerSelection()
        {
            foreach (var item in AllProductPickerItems)
            {
                var line = DraftLines.FirstOrDefault(line => line.IdProducte == item.Id && (!DraftDefaultLocationId.HasValue || line.IdUbicacio == DraftDefaultLocationId));
                item.IsSelected = line is not null;
                if (line is not null)
                {
                    item.PaletsValue = line.Palets ?? 0;
                    item.CaixesValue = Math.Max(1, line.Caixes);
                }
            }
        }

        private void SetDraftValue(Action<OrderRecord> update)
        {
            if (Draft is null)
            {
                return;
            }

            update(Draft);
            RaiseDraftProperties();
        }

        private void RaiseDraftProperties()
        {
            OnPropertyChanged(nameof(DraftIdClient));
            OnPropertyChanged(nameof(DraftIdChofer));
            OnPropertyChanged(nameof(DraftIdPreparador));
            OnPropertyChanged(nameof(DraftIdVehicleTransportista));
            OnPropertyChanged(nameof(DraftIdEstat));
            OnPropertyChanged(nameof(DraftNotes));
            OnPropertyChanged(nameof(DraftPoblacioEntregaAlternativa));
            OnPropertyChanged(nameof(DraftAdrecaEntregaAlternativa));
            OnPropertyChanged(nameof(PanelSubtitle));
        }

        private void RaiseMetrics()
        {
            OnPropertyChanged(nameof(TotalOrders));
            OnPropertyChanged(nameof(PendingOrders));
            OnPropertyChanged(nameof(OpenOrders));
        }

        private int NextDraftLineId()
        {
            return DraftLines.Count == 0 ? -1 : Math.Min(-1, DraftLines.Min(line => line.Id) - 1);
        }

        private static int FirstId(IEnumerable<LookupOption> options)
        {
            return options.FirstOrDefault(option => option.Id.HasValue)?.Id ?? 0;
        }

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
            Lines = record.Lines.Select(CloneLine).ToList(),
            SyncState = record.SyncState,
            SyncMessage = record.SyncMessage
        };

        private static OrderLineRecord CloneLine(OrderLineRecord line) => new()
        {
            Id = line.Id,
            IdProducte = line.IdProducte,
            ProducteText = line.ProducteText,
            IdUbicacio = line.IdUbicacio,
            Ubicacio = line.Ubicacio,
            Palets = line.Palets,
            Caixes = line.Caixes,
            IdEstatVerificacio = line.IdEstatVerificacio
        };
    }
}
