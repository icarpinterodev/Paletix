using PaletixDesktop.Models;
using PaletixDesktop.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using PaletixDesktop.Services;

namespace PaletixDesktop.ViewModels
{
    public enum ProductCatalogViewMode
    {
        Table,
        Grid
    }

    public enum ProductPanelMode
    {
        None,
        Create,
        Edit
    }

    public sealed class ProductCatalogViewModel : ViewModelBase
    {
        private readonly ProductDataService _productDataService;
        private readonly LookupDataService _lookupDataService;
        private readonly ShellViewModel _shell;
        private ProductCatalogViewMode _viewMode = ProductCatalogViewMode.Table;
        private ProductPanelMode _panelMode = ProductPanelMode.None;
        private string _searchText = "";
        private string _statusText = "Carregant productes...";
        private int _editIndex;
        private string _draftReferencia = "";
        private string _draftNom = "";
        private string _draftDescripcio = "";
        private string _draftIdTipus = "1";
        private string _draftVolumMl = "";
        private string _draftIdProveidor = "1";
        private string _draftIdUbicacio = "1";
        private string _draftCaixesPerPalet = "1";
        private string _draftImatgeUrl = "";
        private string _draftActiu = "1";
        private string _draftPreuVendaCaixa = "0";
        private string _draftCostPerCaixa = "0";
        private string _draftEstabilitatAlPalet = "";
        private string _draftPesKg = "";
        private string _validationMessage = "";

        public ProductCatalogViewModel()
            : this(App.CurrentServices.ProductDataService, App.CurrentServices.LookupDataService, App.CurrentServices.ShellViewModel)
        {
        }

        public ProductCatalogViewModel(
            ProductDataService productDataService,
            LookupDataService lookupDataService,
            ShellViewModel shell)
        {
            _productDataService = productDataService;
            _lookupDataService = lookupDataService;
            _shell = shell;
        }

        public ObservableCollection<ProductRecord> Products { get; } = new();
        public ObservableCollection<ProductRecord> FilteredProducts { get; } = new();
        public ObservableCollection<ProductRecord> SelectedProducts { get; } = new();
        public ObservableCollection<LookupOption> ProductTypeOptions { get; } = new();
        public ObservableCollection<LookupOption> SupplierOptions { get; } = new();
        public ObservableCollection<LookupOption> LocationOptions { get; } = new();
        public ObservableCollection<LookupOption> ActiveOptions { get; } = new()
        {
            new LookupOption { Id = 1, Label = "Actiu" },
            new LookupOption { Id = 0, Label = "Inactiu" }
        };

        public ProductCatalogViewMode ViewMode
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

        public ProductPanelMode PanelMode
        {
            get => _panelMode;
            private set
            {
                if (SetProperty(ref _panelMode, value))
                {
                    OnPropertyChanged(nameof(IsPanelOpen));
                    OnPropertyChanged(nameof(PanelTitle));
                    OnPropertyChanged(nameof(PanelSubtitle));
                    OnPropertyChanged(nameof(IsEditingMultiple));
                }
            }
        }

        public bool IsTableView => ViewMode == ProductCatalogViewMode.Table;
        public bool IsGridView => ViewMode == ProductCatalogViewMode.Grid;
        public bool IsPanelOpen => PanelMode != ProductPanelMode.None;
        public bool IsEditingMultiple => PanelMode == ProductPanelMode.Edit && SelectedProducts.Count > 1;
        public string PanelTitle => PanelMode == ProductPanelMode.Edit ? "Editar producte" : "Nou producte";
        public string PanelSubtitle => PanelMode == ProductPanelMode.Edit
            ? $"Producte {EditPositionText}"
            : "Alta local preparada per connectar amb API i SQLite.";
        public string EditPositionText => SelectedProducts.Count == 0 ? "" : $"{_editIndex + 1} de {SelectedProducts.Count}";

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

        public string DraftReferencia { get => _draftReferencia; set => SetDraftProperty(ref _draftReferencia, value); }
        public string DraftNom { get => _draftNom; set => SetDraftProperty(ref _draftNom, value); }
        public string DraftDescripcio { get => _draftDescripcio; set => SetDraftProperty(ref _draftDescripcio, value); }
        public string DraftIdTipus { get => _draftIdTipus; set => SetDraftProperty(ref _draftIdTipus, value); }
        public string DraftVolumMl { get => _draftVolumMl; set => SetDraftProperty(ref _draftVolumMl, value, numericPropertyName: nameof(DraftVolumMlValue)); }
        public string DraftIdProveidor { get => _draftIdProveidor; set => SetDraftProperty(ref _draftIdProveidor, value); }
        public string DraftIdUbicacio { get => _draftIdUbicacio; set => SetDraftProperty(ref _draftIdUbicacio, value); }
        public string DraftCaixesPerPalet { get => _draftCaixesPerPalet; set => SetDraftProperty(ref _draftCaixesPerPalet, value, numericPropertyName: nameof(DraftCaixesPerPaletValue)); }
        public string DraftImatgeUrl
        {
            get => _draftImatgeUrl;
            set
            {
                if (SetProperty(ref _draftImatgeUrl, value))
                {
                    OnPropertyChanged(nameof(ImagePreviewUrl));
                    OnPropertyChanged(nameof(HasImagePreview));
                    if (IsPanelOpen)
                    {
                        ValidateDraft();
                    }
                }
            }
        }
        public string DraftActiu { get => _draftActiu; set => SetDraftProperty(ref _draftActiu, value); }
        public string DraftPreuVendaCaixa { get => _draftPreuVendaCaixa; set => SetDraftProperty(ref _draftPreuVendaCaixa, value, numericPropertyName: nameof(DraftPreuVendaCaixaValue)); }
        public string DraftCostPerCaixa { get => _draftCostPerCaixa; set => SetDraftProperty(ref _draftCostPerCaixa, value, numericPropertyName: nameof(DraftCostPerCaixaValue)); }
        public string DraftEstabilitatAlPalet { get => _draftEstabilitatAlPalet; set => SetDraftProperty(ref _draftEstabilitatAlPalet, value, numericPropertyName: nameof(DraftEstabilitatAlPaletValue)); }
        public string DraftPesKg { get => _draftPesKg; set => SetDraftProperty(ref _draftPesKg, value, numericPropertyName: nameof(DraftPesKgValue)); }
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
        public string ImagePreviewUrl => DraftImatgeUrl.Trim();
        public bool HasImagePreview => !string.IsNullOrWhiteSpace(DraftImatgeUrl) && InputValidation.IsValidImageUrl(DraftImatgeUrl);
        public double DraftVolumMlValue { get => ToNumberBoxValue(DraftVolumMl); set => SetNumberDraft(nameof(DraftVolumMl), value); }
        public double DraftCaixesPerPaletValue { get => ToNumberBoxValue(DraftCaixesPerPalet); set => SetNumberDraft(nameof(DraftCaixesPerPalet), value); }
        public double DraftPreuVendaCaixaValue { get => ToNumberBoxValue(DraftPreuVendaCaixa); set => SetNumberDraft(nameof(DraftPreuVendaCaixa), value); }
        public double DraftCostPerCaixaValue { get => ToNumberBoxValue(DraftCostPerCaixa); set => SetNumberDraft(nameof(DraftCostPerCaixa), value); }
        public double DraftEstabilitatAlPaletValue { get => ToNumberBoxValue(DraftEstabilitatAlPalet); set => SetNumberDraft(nameof(DraftEstabilitatAlPalet), value); }
        public double DraftPesKgValue { get => ToNumberBoxValue(DraftPesKg); set => SetNumberDraft(nameof(DraftPesKg), value); }

        public int TotalCount => Products.Count;
        public int FilteredCount => FilteredProducts.Count;
        public int SelectedCount => SelectedProducts.Count;
        public int ActiveCount => Products.Count(product => product.Actiu == 1);
        public int MissingImageCount => Products.Count(product => string.IsNullOrWhiteSpace(product.ImatgeUrl));
        public int PendingCount => Products.Count(product => product.IsPending);
        public string SelectedCountText => $"{SelectedCount} seleccionat(s)";

        public async Task LoadAsync()
        {
            IsBusy = true;
            ErrorMessage = null;

            try
            {
                await LoadLookupsAsync();

                var cached = await _productDataService.LoadCachedAsync();
                if (cached.Products.Count > 0)
                {
                    ReplaceProducts(cached.Products);
                    StatusText = cached.Message;
                }

                var result = await _productDataService.LoadAsync();
                ReplaceProducts(result.Products);
                StatusText = result.Message;
                await _shell.RefreshSyncStatusAsync(result.IsOnline, result.IsOnline ? "Sincronitzat" : "Mode offline");
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                StatusText = $"No s'han pogut carregar productes: {ex.Message}";
                await _shell.RefreshSyncStatusAsync(false, "Mode offline");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task LoadLookupsAsync()
        {
            await LoadLookupCollectionAsync(ProductTypeOptions, await _lookupDataService.GetProductTypesAsync(includeEmpty: false));
            await LoadLookupCollectionAsync(SupplierOptions, await _lookupDataService.GetSuppliersAsync());
            await LoadLookupCollectionAsync(LocationOptions, await _lookupDataService.GetLocationsAsync());
        }

        public void SetViewMode(ProductCatalogViewMode mode)
        {
            ViewMode = mode;
            StatusText = mode == ProductCatalogViewMode.Table ? "Vista de taula activada." : "Vista de grid activada.";
        }

        public void SetSelection(IEnumerable<ProductRecord> products)
        {
            foreach (var product in Products)
            {
                product.IsSelected = false;
            }

            SelectedProducts.Clear();
            foreach (var product in products.Distinct())
            {
                product.IsSelected = true;
                SelectedProducts.Add(product);
            }

            if (_editIndex >= SelectedProducts.Count)
            {
                _editIndex = Math.Max(0, SelectedProducts.Count - 1);
            }

            RaiseSelectionProperties();
        }

        public void SelectSingle(ProductRecord product)
        {
            SetSelection(new[] { product });
        }

        public void CreateProduct()
        {
            var next = Products.Count + 1;
            LoadDraft(Create(
                0,
                $"REF-{next:000}",
                "",
                "",
                FirstLookupValue(ProductTypeOptions),
                null,
                FirstLookupValue(SupplierOptions),
                FirstLookupValue(LocationOptions),
                1,
                null,
                1,
                0m,
                0m,
                null,
                null,
                null));
            PanelMode = ProductPanelMode.Create;
            StatusText = "Introdueix les dades del nou producte al panell lateral.";
        }

        public void StartEditSelected()
        {
            if (SelectedProducts.Count == 0)
            {
                StatusText = "Selecciona un o mes productes per editar.";
                return;
            }

            _editIndex = 0;
            LoadDraft(SelectedProducts[_editIndex]);
            PanelMode = ProductPanelMode.Edit;
            RaiseSelectionProperties();
        }

        public void MoveEdit(int delta)
        {
            if (SelectedProducts.Count == 0)
            {
                return;
            }

            SaveDraftInto(SelectedProducts[_editIndex]);
            _editIndex = (_editIndex + delta + SelectedProducts.Count) % SelectedProducts.Count;
            LoadDraft(SelectedProducts[_editIndex]);
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

            if (PanelMode == ProductPanelMode.Create)
            {
                var draft = CreateFromDraft(0);
                var result = await _productDataService.CreateAsync(draft, Products.ToList());
                ApplyMutation(result);
                if (result.Product is not null)
                {
                    SelectSingle(Products.First(product => product.Id == result.Product.Id));
                }
            }
            else if (PanelMode == ProductPanelMode.Edit && SelectedProducts.Count > 0)
            {
                SaveDraftInto(SelectedProducts[_editIndex]);
                var selectedIds = SelectedProducts.Select(product => product.Id).ToList();
                ProductMutationResult? lastResult = null;

                foreach (var selected in SelectedProducts.ToList())
                {
                    var current = Products.FirstOrDefault(product => product.Id == selected.Id) ?? selected;
                    lastResult = await _productDataService.UpdateAsync(current, Products.ToList());
                    ApplyMutation(lastResult, preserveSelectionIds: selectedIds);
                }

                if (lastResult is not null)
                {
                    StatusText = lastResult.Message;
                }
            }

            PanelMode = ProductPanelMode.None;
            ValidationMessage = "";
        }

        public void CancelPanel()
        {
            PanelMode = ProductPanelMode.None;
            ValidationMessage = "";
            StatusText = "Edicio cancelada.";
        }

        public async Task DeleteSelectedAsync()
        {
            if (SelectedProducts.Count == 0)
            {
                StatusText = "Selecciona productes abans d'eliminar.";
                return;
            }

            ProductMutationResult? lastResult = null;
            foreach (var selected in SelectedProducts.ToList())
            {
                var current = Products.FirstOrDefault(product => product.Id == selected.Id) ?? selected;
                lastResult = await _productDataService.DeleteAsync(current, Products.ToList());
                ApplyMutation(lastResult);
            }

            SelectedProducts.Clear();
            RaiseSelectionProperties();
            if (lastResult is not null)
            {
                StatusText = lastResult.Message;
            }
        }

        public async Task ImportProductsAsync()
        {
            var next = Products.Count + 1;
            var draft = Create(
                0,
                $"IMP-{next:000}",
                "Producte importat",
                "Importacio manual des del commandbar",
                FirstLookupValue(ProductTypeOptions),
                null,
                FirstLookupValue(SupplierOptions),
                FirstLookupValue(LocationOptions),
                1,
                null,
                1,
                3.20m,
                2.50m,
                null,
                null,
                DateTime.Today);
            var result = await _productDataService.CreateAsync(draft, Products.ToList());
            ApplyMutation(result);
            if (result.Product is not null)
            {
                SelectSingle(Products.First(product => product.Id == result.Product.Id));
            }

            StatusText = result.Message;
        }

        private void ApplyMutation(ProductMutationResult result, IReadOnlyList<int>? preserveSelectionIds = null)
        {
            ReplaceProducts(result.Products);
            StatusText = result.Message;
            if (preserveSelectionIds is not null)
            {
                SetSelection(Products.Where(product => preserveSelectionIds.Contains(product.Id)));
            }

            _ = _shell.RefreshSyncStatusAsync(null, result.PendingCount == 0 ? "Sincronitzat" : result.Message);
        }

        private void ReplaceProducts(IReadOnlyList<ProductRecord> products)
        {
            Products.Clear();
            foreach (var product in products.OrderByDescending(product => product.Id < 0).ThenBy(product => product.Id))
            {
                Products.Add(product);
            }

            RefreshFilter();
            RaiseMetrics();
        }

        private void RefreshFilter()
        {
            var query = SearchText.Trim().ToLowerInvariant();
            var products = string.IsNullOrWhiteSpace(query)
                ? Products
                : Products.Where(product =>
                    product.IdText.Contains(query) ||
                    (product.Referencia?.ToLowerInvariant().Contains(query) ?? false) ||
                    product.Nom.ToLowerInvariant().Contains(query) ||
                    (product.Descripcio?.ToLowerInvariant().Contains(query) ?? false) ||
                    product.SyncStateText.ToLowerInvariant().Contains(query));

            FilteredProducts.Clear();
            foreach (var product in products)
            {
                FilteredProducts.Add(product);
            }

            SetSelection(SelectedProducts.Where(FilteredProducts.Contains).ToList());
            RaiseMetrics();
        }

        private void LoadDraft(ProductRecord product)
        {
            ValidationMessage = "";
            DraftReferencia = product.Referencia ?? "";
            DraftNom = product.Nom;
            DraftDescripcio = product.Descripcio ?? "";
            DraftIdTipus = product.IdTipus.ToString(CultureInfo.InvariantCulture);
            DraftVolumMl = product.VolumMl?.ToString(CultureInfo.InvariantCulture) ?? "";
            DraftIdProveidor = product.IdProveidor.ToString(CultureInfo.InvariantCulture);
            DraftIdUbicacio = product.IdUbicacio.ToString(CultureInfo.InvariantCulture);
            DraftCaixesPerPalet = product.CaixesPerPalet.ToString(CultureInfo.InvariantCulture);
            DraftImatgeUrl = product.ImatgeUrl ?? "";
            DraftActiu = product.Actiu.ToString(CultureInfo.InvariantCulture);
            DraftPreuVendaCaixa = product.PreuVendaCaixa.ToString(CultureInfo.InvariantCulture);
            DraftCostPerCaixa = product.CostPerCaixa.ToString(CultureInfo.InvariantCulture);
            DraftEstabilitatAlPalet = product.EstabilitatAlPalet?.ToString(CultureInfo.InvariantCulture) ?? "";
            DraftPesKg = product.PesKg?.ToString(CultureInfo.InvariantCulture) ?? "";
        }

        private static System.Threading.Tasks.Task LoadLookupCollectionAsync(
            ObservableCollection<LookupOption> target,
            IReadOnlyList<LookupOption> source)
        {
            target.Clear();
            foreach (var item in source)
            {
                target.Add(item);
            }

            return System.Threading.Tasks.Task.CompletedTask;
        }

        private bool ValidateDraft()
        {
            var errors = new List<string>();
            Require(DraftNom, "Nom", errors);
            Require(DraftIdTipus, "Tipus de producte", errors);
            Require(DraftIdProveidor, "Proveidor", errors);
            Require(DraftIdUbicacio, "Ubicacio", errors);
            Require(DraftCaixesPerPalet, "Caixes per palet", errors);
            Require(DraftPreuVendaCaixa, "Preu de venda per caixa", errors);
            Require(DraftCostPerCaixa, "Cost per caixa", errors);

            MaxLength(DraftReferencia, 50, "Referencia", errors);
            MaxLength(DraftNom, 150, "Nom", errors);
            MaxLength(DraftDescripcio, 300, "Descripcio", errors);
            MaxLength(DraftImatgeUrl, 2048, "URL de la imatge", errors);

            if (!string.IsNullOrWhiteSpace(DraftReferencia) && !InputValidation.IsValidReference(DraftReferencia))
            {
                errors.Add("Referencia nomes pot contenir lletres, numeros, punts, guions, barres i guio baix, i ha de comencar per lletra o numero.");
            }

            ValidateRequiredLookup(DraftIdTipus, ProductTypeOptions, "Tipus de producte", errors);
            ValidateRequiredLookup(DraftIdProveidor, SupplierOptions, "Proveidor", errors);
            ValidateRequiredLookup(DraftIdUbicacio, LocationOptions, "Ubicacio", errors);
            ValidateRequiredLookup(DraftActiu, ActiveOptions, "Actiu", errors);

            ValidateInt(DraftCaixesPerPalet, "Caixes per palet", minValue: 1, errors);
            ValidateDecimal(DraftPreuVendaCaixa, "Preu de venda per caixa", required: true, minValue: 0m, errors);
            ValidateDecimal(DraftCostPerCaixa, "Cost per caixa", required: true, minValue: 0m, errors);
            ValidateDecimal(DraftVolumMl, "Volum (ml)", required: false, minValue: 0m, errors);
            ValidateDecimal(DraftPesKg, "Pes (kg)", required: false, minValue: 0m, errors);
            ValidateInt(DraftEstabilitatAlPalet, "Estabilitat al palet", minValue: 0, errors, required: false);

            if (!string.IsNullOrWhiteSpace(DraftImatgeUrl) && !InputValidation.IsValidImageUrl(DraftImatgeUrl))
            {
                errors.Add("URL de la imatge ha de ser una URL http/https valida, sense espais.");
            }

            ValidationMessage = string.Join("\n", errors);
            return errors.Count == 0;
        }

        private void SetDraftProperty(
            ref string storage,
            string value,
            [CallerMemberName] string? propertyName = null,
            string? numericPropertyName = null)
        {
            if (SetProperty(ref storage, value, propertyName))
            {
                if (!string.IsNullOrWhiteSpace(numericPropertyName))
                {
                    OnPropertyChanged(numericPropertyName);
                }

                if (IsPanelOpen)
                {
                    ValidateDraft();
                }
            }
        }

        private void SetNumberDraft(string propertyName, double value)
        {
            var text = double.IsNaN(value)
                ? ""
                : value.ToString("0.##", CultureInfo.InvariantCulture);

            switch (propertyName)
            {
                case nameof(DraftVolumMl):
                    DraftVolumMl = text;
                    break;
                case nameof(DraftCaixesPerPalet):
                    DraftCaixesPerPalet = text;
                    break;
                case nameof(DraftPreuVendaCaixa):
                    DraftPreuVendaCaixa = text;
                    break;
                case nameof(DraftCostPerCaixa):
                    DraftCostPerCaixa = text;
                    break;
                case nameof(DraftEstabilitatAlPalet):
                    DraftEstabilitatAlPalet = text;
                    break;
                case nameof(DraftPesKg):
                    DraftPesKg = text;
                    break;
            }
        }

        private static double ToNumberBoxValue(string value)
        {
            return InputValidation.TryParseDecimal(value, out var result)
                ? decimal.ToDouble(result)
                : double.NaN;
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

        private static void ValidateRequiredLookup(
            string value,
            IEnumerable<LookupOption> options,
            string label,
            ICollection<string> errors)
        {
            if (string.IsNullOrWhiteSpace(value) || !options.Any(option => option.Value == value))
            {
                errors.Add($"{label} s'ha de triar del desplegable.");
            }
        }

        private static void ValidateInt(
            string value,
            string label,
            int minValue,
            ICollection<string> errors,
            bool required = true)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                if (required)
                {
                    errors.Add($"{label} es obligatori.");
                }

                return;
            }

            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) || parsed < minValue)
            {
                errors.Add($"{label} ha de ser un enter igual o superior a {minValue}.");
            }
        }

        private static void ValidateDecimal(
            string value,
            string label,
            bool required,
            decimal minValue,
            ICollection<string> errors)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                if (required)
                {
                    errors.Add($"{label} es obligatori.");
                }

                return;
            }

            if (!InputValidation.TryParseDecimal(value, out var parsed) || parsed < minValue)
            {
                errors.Add($"{label} ha de ser un numero igual o superior a {minValue.ToString(CultureInfo.InvariantCulture)}.");
            }
        }

        private ProductRecord CreateFromDraft(int id)
        {
            return Create(
                id,
                EmptyToNull(DraftReferencia),
                string.IsNullOrWhiteSpace(DraftNom) ? "Producte sense nom" : DraftNom.Trim(),
                EmptyToNull(DraftDescripcio),
                ParseInt(DraftIdTipus, 1),
                ParseNullableDecimal(DraftVolumMl),
                ParseInt(DraftIdProveidor, 1),
                ParseInt(DraftIdUbicacio, 1),
                ParseInt(DraftCaixesPerPalet, 1),
                EmptyToNull(DraftImatgeUrl),
                (sbyte)ParseInt(DraftActiu, 1),
                ParseDecimal(DraftPreuVendaCaixa),
                ParseDecimal(DraftCostPerCaixa),
                ParseNullableInt(DraftEstabilitatAlPalet),
                ParseNullableDecimal(DraftPesKg),
                DateTime.Today);
        }

        private void SaveDraftInto(ProductRecord product)
        {
            var originalDataAfegit = product.DataAfegit;
            var updated = CreateFromDraft(product.Id);
            product.Referencia = updated.Referencia;
            product.Nom = updated.Nom;
            product.Descripcio = updated.Descripcio;
            product.IdTipus = updated.IdTipus;
            product.VolumMl = updated.VolumMl;
            product.IdProveidor = updated.IdProveidor;
            product.IdUbicacio = updated.IdUbicacio;
            product.CaixesPerPalet = updated.CaixesPerPalet;
            product.ImatgeUrl = updated.ImatgeUrl;
            product.Actiu = updated.Actiu;
            product.PreuVendaCaixa = updated.PreuVendaCaixa;
            product.CostPerCaixa = updated.CostPerCaixa;
            product.EstabilitatAlPalet = updated.EstabilitatAlPalet;
            product.PesKg = updated.PesKg;
            product.DataAfegit = originalDataAfegit;
        }

        private void RaiseMetrics()
        {
            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(FilteredCount));
            OnPropertyChanged(nameof(ActiveCount));
            OnPropertyChanged(nameof(MissingImageCount));
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

        private static ProductRecord Create(
            int id,
            string? referencia,
            string nom,
            string? descripcio,
            int idTipus,
            decimal? volumMl,
            int idProveidor,
            int idUbicacio,
            int caixesPerPalet,
            string? imatgeUrl,
            sbyte actiu,
            decimal preuVendaCaixa,
            decimal costPerCaixa,
            int? estabilitatAlPalet,
            decimal? pesKg,
            DateTime? dataAfegit,
            ProductSyncState syncState = ProductSyncState.Synced,
            string syncMessage = "Sincronitzat")
        {
            return new ProductRecord
            {
                Id = id,
                Referencia = referencia,
                Nom = nom,
                Descripcio = descripcio,
                IdTipus = idTipus,
                VolumMl = volumMl,
                IdProveidor = idProveidor,
                IdUbicacio = idUbicacio,
                CaixesPerPalet = caixesPerPalet,
                ImatgeUrl = imatgeUrl,
                Actiu = actiu,
                PreuVendaCaixa = preuVendaCaixa,
                CostPerCaixa = costPerCaixa,
                EstabilitatAlPalet = estabilitatAlPalet,
                PesKg = pesKg,
                DataAfegit = dataAfegit,
                SyncState = syncState,
                SyncMessage = syncMessage
            };
        }

        private static int FirstLookupValue(IEnumerable<LookupOption> options)
        {
            return options.FirstOrDefault(option => option.Id.HasValue)?.Id ?? 1;
        }

        private static string? EmptyToNull(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static int ParseInt(string value, int fallback)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : fallback;
        }

        private static int? ParseNullableInt(string value)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : null;
        }

        private static decimal ParseDecimal(string value)
        {
            return InputValidation.ParseDecimalOrDefault(value);
        }

        private static decimal? ParseNullableDecimal(string value)
        {
            return InputValidation.TryParseDecimal(value, out var result) ? result : null;
        }
    }
}
