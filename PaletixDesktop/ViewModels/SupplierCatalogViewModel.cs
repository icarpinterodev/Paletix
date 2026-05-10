using PaletixDesktop.Models;
using PaletixDesktop.Services;
using PaletixDesktop.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace PaletixDesktop.ViewModels
{
    public enum SupplierCatalogViewMode
    {
        Table,
        Grid
    }

    public enum SupplierPanelMode
    {
        None,
        Create,
        Edit
    }

    public sealed class SupplierCatalogViewModel : ViewModelBase
    {
        private readonly SupplierDataService _supplierDataService;
        private readonly LookupDataService _lookupDataService;
        private readonly ShellViewModel _shell;
        private SupplierCatalogViewMode _viewMode = SupplierCatalogViewMode.Table;
        private SupplierPanelMode _panelMode = SupplierPanelMode.None;
        private string _searchText = "";
        private string _statusText = "Carregant proveidors...";
        private int _editIndex;
        private string _draftMarcaMatriu = "";
        private string _draftNomEmpresa = "";
        private string _draftTelefon = "";
        private string _draftEmail = "";
        private string _draftAdreca = "";
        private string _draftUrlWeb = "";
        private string _draftIdTipusProductePrincipal = "";
        private string _validationMessage = "";

        public SupplierCatalogViewModel()
            : this(App.CurrentServices.SupplierDataService, App.CurrentServices.LookupDataService, App.CurrentServices.ShellViewModel)
        {
        }

        public SupplierCatalogViewModel(
            SupplierDataService supplierDataService,
            LookupDataService lookupDataService,
            ShellViewModel shell)
        {
            _supplierDataService = supplierDataService;
            _lookupDataService = lookupDataService;
            _shell = shell;
        }

        public ObservableCollection<SupplierRecord> Suppliers { get; } = new();
        public ObservableCollection<SupplierRecord> FilteredSuppliers { get; } = new();
        public ObservableCollection<SupplierRecord> SelectedSuppliers { get; } = new();
        public ObservableCollection<LookupOption> ProductTypeOptions { get; } = new();

        public bool IsTableView => ViewMode == SupplierCatalogViewMode.Table;
        public bool IsGridView => ViewMode == SupplierCatalogViewMode.Grid;
        public bool IsPanelOpen => PanelMode != SupplierPanelMode.None;
        public bool IsEditingMultiple => PanelMode == SupplierPanelMode.Edit && SelectedSuppliers.Count > 1;
        public string PanelTitle => PanelMode == SupplierPanelMode.Edit ? "Editar proveidor" : "Nou proveidor";
        public string PanelSubtitle => PanelMode == SupplierPanelMode.Edit ? $"Proveidor {EditPositionText}" : "Alta preparada per API, SQLite i mode offline.";
        public string EditPositionText => SelectedSuppliers.Count == 0 ? "" : $"{_editIndex + 1} de {SelectedSuppliers.Count}";

        public SupplierCatalogViewMode ViewMode
        {
            get => _viewMode;
            private set
            {
                if (SetProperty(ref _viewMode, value))
                {
                    OnPropertyChanged(nameof(IsTableView));
                    OnPropertyChanged(nameof(IsGridView));
                }
            }
        }

        public SupplierPanelMode PanelMode
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

        public string StatusText { get => _statusText; private set => SetProperty(ref _statusText, value); }
        public string DraftMarcaMatriu { get => _draftMarcaMatriu; set => SetDraftProperty(ref _draftMarcaMatriu, value); }
        public string DraftNomEmpresa { get => _draftNomEmpresa; set => SetDraftProperty(ref _draftNomEmpresa, value); }
        public string DraftTelefon { get => _draftTelefon; set => SetDraftProperty(ref _draftTelefon, value); }
        public string DraftEmail { get => _draftEmail; set => SetDraftProperty(ref _draftEmail, value); }
        public string DraftAdreca { get => _draftAdreca; set => SetDraftProperty(ref _draftAdreca, value); }
        public string DraftUrlWeb { get => _draftUrlWeb; set => SetDraftProperty(ref _draftUrlWeb, value); }
        public string DraftIdTipusProductePrincipal { get => _draftIdTipusProductePrincipal; set => SetDraftProperty(ref _draftIdTipusProductePrincipal, value); }
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

        public int TotalCount => Suppliers.Count;
        public int FilteredCount => FilteredSuppliers.Count;
        public int SelectedCount => SelectedSuppliers.Count;
        public int WithWebCount => Suppliers.Count(supplier => !string.IsNullOrWhiteSpace(supplier.UrlWeb));
        public int WithoutParentBrandCount => Suppliers.Count(supplier => string.IsNullOrWhiteSpace(supplier.MarcaMatriu));
        public int PendingCount => Suppliers.Count(supplier => supplier.IsPending);
        public string SelectedCountText => $"{SelectedCount} seleccionat(s)";

        public async Task LoadAsync()
        {
            IsBusy = true;
            ErrorMessage = null;

            try
            {
                await LoadLookupsAsync();

                var cached = await _supplierDataService.LoadCachedAsync();
                if (cached.Suppliers.Count > 0)
                {
                    ReplaceSuppliers(cached.Suppliers);
                    StatusText = cached.Message;
                }

                var result = await _supplierDataService.LoadAsync();
                ReplaceSuppliers(result.Suppliers);
                StatusText = result.Message;
                await _shell.RefreshSyncStatusAsync(result.IsOnline, result.IsOnline ? "Sincronitzat" : "Mode offline");
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                StatusText = $"No s'han pogut carregar proveidors: {ex.Message}";
                await _shell.RefreshSyncStatusAsync(false, "Mode offline");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void SetViewMode(SupplierCatalogViewMode mode)
        {
            ViewMode = mode;
            StatusText = mode == SupplierCatalogViewMode.Table ? "Vista de taula activada." : "Vista de grid activada.";
        }

        public void SetSelection(IEnumerable<SupplierRecord> suppliers)
        {
            foreach (var supplier in Suppliers)
            {
                supplier.IsSelected = false;
            }

            SelectedSuppliers.Clear();
            foreach (var supplier in suppliers.Distinct())
            {
                supplier.IsSelected = true;
                SelectedSuppliers.Add(supplier);
            }

            if (_editIndex >= SelectedSuppliers.Count)
            {
                _editIndex = Math.Max(0, SelectedSuppliers.Count - 1);
            }

            RaiseSelectionProperties();
        }

        public void SelectSingle(SupplierRecord supplier)
        {
            SetSelection(new[] { supplier });
        }

        public void CreateSupplier()
        {
            var next = Suppliers.Count + 1;
            LoadDraft(Create(0, "", $"Proveidor {next:000}", "", "", "", "", null, SupplierSyncState.Synced, "Nou proveidor"));
            PanelMode = SupplierPanelMode.Create;
            StatusText = "Introdueix les dades del nou proveidor al panell lateral.";
        }

        public void StartEditSelected()
        {
            if (SelectedSuppliers.Count == 0)
            {
                StatusText = "Selecciona un o mes proveidors per editar.";
                return;
            }

            _editIndex = 0;
            LoadDraft(SelectedSuppliers[_editIndex]);
            PanelMode = SupplierPanelMode.Edit;
            RaiseSelectionProperties();
        }

        public void MoveEdit(int delta)
        {
            if (SelectedSuppliers.Count == 0)
            {
                return;
            }

            SaveDraftInto(SelectedSuppliers[_editIndex]);
            _editIndex = (_editIndex + delta + SelectedSuppliers.Count) % SelectedSuppliers.Count;
            LoadDraft(SelectedSuppliers[_editIndex]);
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

            if (PanelMode == SupplierPanelMode.Create)
            {
                var draft = CreateFromDraft(0);
                var result = await _supplierDataService.CreateAsync(draft, Suppliers.ToList());
                ApplyMutation(result);
                if (result.Supplier is not null)
                {
                    SelectSingle(Suppliers.First(supplier => supplier.Id == result.Supplier.Id));
                }
            }
            else if (PanelMode == SupplierPanelMode.Edit && SelectedSuppliers.Count > 0)
            {
                SaveDraftInto(SelectedSuppliers[_editIndex]);
                var selectedIds = SelectedSuppliers.Select(supplier => supplier.Id).ToList();
                SupplierMutationResult? lastResult = null;

                foreach (var selected in SelectedSuppliers.ToList())
                {
                    var current = Suppliers.FirstOrDefault(supplier => supplier.Id == selected.Id) ?? selected;
                    lastResult = await _supplierDataService.UpdateAsync(current, Suppliers.ToList());
                    ApplyMutation(lastResult, preserveSelectionIds: selectedIds);
                }

                if (lastResult is not null)
                {
                    StatusText = lastResult.Message;
                }
            }

            PanelMode = SupplierPanelMode.None;
            ValidationMessage = "";
        }

        public void CancelPanel()
        {
            PanelMode = SupplierPanelMode.None;
            ValidationMessage = "";
            StatusText = "Edicio cancelada.";
        }

        public async Task DeleteSelectedAsync()
        {
            if (SelectedSuppliers.Count == 0)
            {
                StatusText = "Selecciona proveidors abans d'eliminar.";
                return;
            }

            SupplierMutationResult? lastResult = null;
            foreach (var selected in SelectedSuppliers.ToList())
            {
                var current = Suppliers.FirstOrDefault(supplier => supplier.Id == selected.Id) ?? selected;
                lastResult = await _supplierDataService.DeleteAsync(current, Suppliers.ToList());
                ApplyMutation(lastResult);
            }

            SelectedSuppliers.Clear();
            RaiseSelectionProperties();
            if (lastResult is not null)
            {
                StatusText = lastResult.Message;
            }
        }

        public void CompareSelected()
        {
            StatusText = SelectedSuppliers.Count < 2
                ? "Selecciona dos o mes proveidors per comparar-los."
                : $"Comparacio pendent per {SelectedSuppliers.Count} proveidors seleccionats.";
        }

        private void ApplyMutation(SupplierMutationResult result, IReadOnlyList<int>? preserveSelectionIds = null)
        {
            ReplaceSuppliers(result.Suppliers);
            StatusText = result.Message;
            if (preserveSelectionIds is not null)
            {
                SetSelection(Suppliers.Where(supplier => preserveSelectionIds.Contains(supplier.Id)));
            }

            _ = _shell.RefreshSyncStatusAsync(null, result.PendingCount == 0 ? "Sincronitzat" : result.Message);
        }

        private void ReplaceSuppliers(IReadOnlyList<SupplierRecord> suppliers)
        {
            Suppliers.Clear();
            foreach (var supplier in suppliers.OrderByDescending(supplier => supplier.Id < 0).ThenBy(supplier => supplier.Id))
            {
                Suppliers.Add(supplier);
            }

            RefreshFilter();
            RaiseMetrics();
        }

        private void RefreshFilter()
        {
            var query = SearchText.Trim().ToLowerInvariant();
            var suppliers = string.IsNullOrWhiteSpace(query)
                ? Suppliers
                : Suppliers.Where(supplier =>
                    supplier.IdText.Contains(query) ||
                    supplier.NomEmpresa.ToLowerInvariant().Contains(query) ||
                    (supplier.MarcaMatriu?.ToLowerInvariant().Contains(query) ?? false) ||
                    supplier.Telefon.ToLowerInvariant().Contains(query) ||
                    supplier.Email.ToLowerInvariant().Contains(query) ||
                    (supplier.Adreca?.ToLowerInvariant().Contains(query) ?? false) ||
                    (supplier.UrlWeb?.ToLowerInvariant().Contains(query) ?? false) ||
                    supplier.IdTipusProductePrincipalText.Contains(query) ||
                    supplier.SyncStateText.ToLowerInvariant().Contains(query));

            FilteredSuppliers.Clear();
            foreach (var supplier in suppliers)
            {
                FilteredSuppliers.Add(supplier);
            }

            SetSelection(SelectedSuppliers.Where(FilteredSuppliers.Contains).ToList());
            RaiseMetrics();
        }

        private void LoadDraft(SupplierRecord supplier)
        {
            ValidationMessage = "";
            DraftMarcaMatriu = supplier.MarcaMatriu ?? "";
            DraftNomEmpresa = supplier.NomEmpresa;
            DraftTelefon = supplier.Telefon;
            DraftEmail = supplier.Email;
            DraftAdreca = supplier.Adreca ?? "";
            DraftUrlWeb = supplier.UrlWeb ?? "";
            DraftIdTipusProductePrincipal = supplier.IdTipusProductePrincipal?.ToString(CultureInfo.InvariantCulture) ?? "";
        }

        private async Task LoadLookupsAsync()
        {
            var types = await _lookupDataService.GetProductTypesAsync(includeEmpty: true);
            ProductTypeOptions.Clear();
            foreach (var item in types)
            {
                ProductTypeOptions.Add(item);
            }
        }

        private bool ValidateDraft()
        {
            var errors = new List<string>();
            Require(DraftNomEmpresa, "Empresa", errors);
            Require(DraftTelefon, "Telefon", errors);
            Require(DraftEmail, "Correu electronic", errors);

            MaxLength(DraftMarcaMatriu, 100, "Marca matriu", errors);
            MaxLength(DraftNomEmpresa, 100, "Empresa", errors);
            MaxLength(DraftTelefon, 16, "Telefon", errors);
            MaxLength(DraftEmail, 200, "Correu electronic", errors);
            MaxLength(DraftUrlWeb, 2048, "Enllac a la pagina web", errors);

            if (!string.IsNullOrWhiteSpace(DraftTelefon) && !InputValidation.IsValidPhone(DraftTelefon, 16))
            {
                errors.Add("Telefon ha de tenir un format valid, amb 6 a 15 digits i nomes +, espais, punts, guions o parentesis.");
            }

            if (!string.IsNullOrWhiteSpace(DraftEmail) && !InputValidation.IsValidEmail(DraftEmail))
            {
                errors.Add("Correu electronic ha de tenir un format valid.");
            }

            if (!string.IsNullOrWhiteSpace(DraftUrlWeb) && !InputValidation.IsValidHttpUrl(DraftUrlWeb))
            {
                errors.Add("Enllac a la pagina web ha de ser una URL http/https valida, sense espais.");
            }

            if (!string.IsNullOrWhiteSpace(DraftIdTipusProductePrincipal) &&
                ProductTypeOptions.Count > 0 &&
                ProductTypeOptions.All(option => option.Value != DraftIdTipusProductePrincipal))
            {
                errors.Add("Tipus de producte principal ha de ser un valor del desplegable.");
            }

            ValidationMessage = string.Join("\n", errors);
            return errors.Count == 0;
        }

        private void SetDraftProperty(ref string storage, string value, [CallerMemberName] string? propertyName = null)
        {
            if (SetProperty(ref storage, value, propertyName) && IsPanelOpen)
            {
                ValidateDraft();
            }
        }

        private static void Require(string value, string label, ICollection<string> errors)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add($"{label} es obligatori.");
            }
        }

        private static void MaxLength(string value, int maxLength, string label, ICollection<string> errors)
        {
            if (value.Length > maxLength)
            {
                errors.Add($"{label} no pot superar {maxLength} caracters.");
            }
        }

        private SupplierRecord CreateFromDraft(int id)
        {
            return Create(
                id,
                EmptyToNull(DraftMarcaMatriu),
                string.IsNullOrWhiteSpace(DraftNomEmpresa) ? "Proveidor sense nom" : DraftNomEmpresa.Trim(),
                string.IsNullOrWhiteSpace(DraftTelefon) ? "-" : DraftTelefon.Trim(),
                string.IsNullOrWhiteSpace(DraftEmail) ? "-" : DraftEmail.Trim(),
                EmptyToNull(DraftAdreca),
                EmptyToNull(DraftUrlWeb),
                ParseNullableInt(DraftIdTipusProductePrincipal),
                SupplierSyncState.Synced,
                "Preparat");
        }

        private void SaveDraftInto(SupplierRecord supplier)
        {
            var updated = CreateFromDraft(supplier.Id);
            supplier.MarcaMatriu = updated.MarcaMatriu;
            supplier.NomEmpresa = updated.NomEmpresa;
            supplier.Telefon = updated.Telefon;
            supplier.Email = updated.Email;
            supplier.Adreca = updated.Adreca;
            supplier.UrlWeb = updated.UrlWeb;
            supplier.IdTipusProductePrincipal = updated.IdTipusProductePrincipal;
        }

        private void RaiseMetrics()
        {
            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(FilteredCount));
            OnPropertyChanged(nameof(WithWebCount));
            OnPropertyChanged(nameof(WithoutParentBrandCount));
            OnPropertyChanged(nameof(PendingCount));
        }

        private void RaiseSelectionProperties()
        {
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(SelectedCountText));
            OnPropertyChanged(nameof(IsEditingMultiple));
            OnPropertyChanged(nameof(EditPositionText));
            OnPropertyChanged(nameof(PanelSubtitle));
        }

        private static SupplierRecord Create(
            int id,
            string? marcaMatriu,
            string nomEmpresa,
            string telefon,
            string email,
            string? adreca,
            string? urlWeb,
            int? idTipusProductePrincipal,
            SupplierSyncState syncState,
            string syncMessage)
        {
            return new SupplierRecord
            {
                Id = id,
                MarcaMatriu = marcaMatriu,
                NomEmpresa = nomEmpresa,
                Telefon = telefon,
                Email = email,
                Adreca = adreca,
                UrlWeb = urlWeb,
                IdTipusProductePrincipal = idTipusProductePrincipal,
                SyncState = syncState,
                SyncMessage = syncMessage
            };
        }

        private static string? EmptyToNull(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static int? ParseNullableInt(string value)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : null;
        }
    }
}
