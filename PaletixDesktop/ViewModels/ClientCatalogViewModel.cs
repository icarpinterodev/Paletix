using PaletixDesktop.Models;
using PaletixDesktop.Services;
using PaletixDesktop.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace PaletixDesktop.ViewModels
{
    public enum ClientCatalogViewMode
    {
        Table,
        Grid
    }

    public enum ClientPanelMode
    {
        None,
        Create,
        Edit
    }

    public sealed class ClientCatalogViewModel : ViewModelBase
    {
        private readonly ClientDataService _clientDataService;
        private readonly ShellViewModel _shell;
        private ClientCatalogViewMode _viewMode = ClientCatalogViewMode.Table;
        private ClientPanelMode _panelMode = ClientPanelMode.None;
        private string _searchText = "";
        private string _statusText = "Carregant clients...";
        private int _editIndex;
        private string _draftNomEmpresa = "";
        private string _draftNifEmpresa = "";
        private string _draftTelefon = "";
        private string _draftEmail = "";
        private string _draftAdreca = "";
        private string _draftPoblacio = "";
        private string _draftNomResponsable = "";
        private string _validationMessage = "";

        public ClientCatalogViewModel()
            : this(App.CurrentServices.ClientDataService, App.CurrentServices.ShellViewModel)
        {
        }

        public ClientCatalogViewModel(ClientDataService clientDataService, ShellViewModel shell)
        {
            _clientDataService = clientDataService;
            _shell = shell;
        }

        public ObservableCollection<ClientRecord> Clients { get; } = new();
        public ObservableCollection<ClientRecord> FilteredClients { get; } = new();
        public ObservableCollection<ClientRecord> SelectedClients { get; } = new();

        public bool IsTableView => ViewMode == ClientCatalogViewMode.Table;
        public bool IsGridView => ViewMode == ClientCatalogViewMode.Grid;
        public bool IsPanelOpen => PanelMode != ClientPanelMode.None;
        public bool IsEditingMultiple => PanelMode == ClientPanelMode.Edit && SelectedClients.Count > 1;
        public string PanelTitle => PanelMode == ClientPanelMode.Edit ? "Editar client" : "Nou client";
        public string PanelSubtitle => PanelMode == ClientPanelMode.Edit ? $"Client {EditPositionText}" : "Alta preparada per API, SQLite i mode offline.";
        public string EditPositionText => SelectedClients.Count == 0 ? "" : $"{_editIndex + 1} de {SelectedClients.Count}";

        public ClientCatalogViewMode ViewMode
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

        public ClientPanelMode PanelMode
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
        public string DraftNomEmpresa { get => _draftNomEmpresa; set => SetDraftProperty(ref _draftNomEmpresa, value); }
        public string DraftNifEmpresa { get => _draftNifEmpresa; set => SetDraftProperty(ref _draftNifEmpresa, value); }
        public string DraftTelefon { get => _draftTelefon; set => SetDraftProperty(ref _draftTelefon, value); }
        public string DraftEmail { get => _draftEmail; set => SetDraftProperty(ref _draftEmail, value); }
        public string DraftAdreca { get => _draftAdreca; set => SetDraftProperty(ref _draftAdreca, value); }
        public string DraftPoblacio { get => _draftPoblacio; set => SetDraftProperty(ref _draftPoblacio, value); }
        public string DraftNomResponsable { get => _draftNomResponsable; set => SetDraftProperty(ref _draftNomResponsable, value); }
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

        public int TotalCount => Clients.Count;
        public int FilteredCount => FilteredClients.Count;
        public int SelectedCount => SelectedClients.Count;
        public int WithEmailCount => Clients.Count(client => !string.IsNullOrWhiteSpace(client.Email));
        public int WithoutResponsibleCount => Clients.Count(client => string.IsNullOrWhiteSpace(client.NomResponsable));
        public int PendingCount => Clients.Count(client => client.IsPending);
        public string SelectedCountText => $"{SelectedCount} seleccionat(s)";

        public async Task LoadAsync()
        {
            IsBusy = true;
            ErrorMessage = null;

            try
            {
                var cached = await _clientDataService.LoadCachedAsync();
                if (cached.Clients.Count > 0)
                {
                    ReplaceClients(cached.Clients);
                    StatusText = cached.Message;
                }

                var result = await _clientDataService.LoadAsync();
                ReplaceClients(result.Clients);
                StatusText = result.Message;
                await _shell.RefreshSyncStatusAsync(result.IsOnline, result.IsOnline ? "Sincronitzat" : "Mode offline");
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                StatusText = $"No s'han pogut carregar clients: {ex.Message}";
                await _shell.RefreshSyncStatusAsync(false, "Mode offline");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void SetViewMode(ClientCatalogViewMode mode)
        {
            ViewMode = mode;
            StatusText = mode == ClientCatalogViewMode.Table ? "Vista de taula activada." : "Vista de grid activada.";
        }

        public void SetSelection(IEnumerable<ClientRecord> clients)
        {
            foreach (var client in Clients)
            {
                client.IsSelected = false;
            }

            SelectedClients.Clear();
            foreach (var client in clients.Distinct())
            {
                client.IsSelected = true;
                SelectedClients.Add(client);
            }

            if (_editIndex >= SelectedClients.Count)
            {
                _editIndex = Math.Max(0, SelectedClients.Count - 1);
            }

            RaiseSelectionProperties();
        }

        public void SelectSingle(ClientRecord client)
        {
            SetSelection(new[] { client });
        }

        public void CreateClient()
        {
            var next = Clients.Count + 1;
            LoadDraft(Create(0, $"Client {next:000}", "", "", "", "", "", "", ClientSyncState.Synced, "Nou client"));
            PanelMode = ClientPanelMode.Create;
            StatusText = "Introdueix les dades del nou client al panell lateral.";
        }

        public void StartEditSelected()
        {
            if (SelectedClients.Count == 0)
            {
                StatusText = "Selecciona un o mes clients per editar.";
                return;
            }

            _editIndex = 0;
            LoadDraft(SelectedClients[_editIndex]);
            PanelMode = ClientPanelMode.Edit;
            RaiseSelectionProperties();
        }

        public void MoveEdit(int delta)
        {
            if (SelectedClients.Count == 0)
            {
                return;
            }

            SaveDraftInto(SelectedClients[_editIndex]);
            _editIndex = (_editIndex + delta + SelectedClients.Count) % SelectedClients.Count;
            LoadDraft(SelectedClients[_editIndex]);
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

            if (PanelMode == ClientPanelMode.Create)
            {
                var draft = CreateFromDraft(0);
                var result = await _clientDataService.CreateAsync(draft, Clients.ToList());
                ApplyMutation(result);
                if (result.Client is not null)
                {
                    SelectSingle(Clients.First(client => client.Id == result.Client.Id));
                }
            }
            else if (PanelMode == ClientPanelMode.Edit && SelectedClients.Count > 0)
            {
                SaveDraftInto(SelectedClients[_editIndex]);
                var selectedIds = SelectedClients.Select(client => client.Id).ToList();
                ClientMutationResult? lastResult = null;

                foreach (var selected in SelectedClients.ToList())
                {
                    var current = Clients.FirstOrDefault(client => client.Id == selected.Id) ?? selected;
                    lastResult = await _clientDataService.UpdateAsync(current, Clients.ToList());
                    ApplyMutation(lastResult, preserveSelectionIds: selectedIds);
                }

                if (lastResult is not null)
                {
                    StatusText = lastResult.Message;
                }
            }

            PanelMode = ClientPanelMode.None;
            ValidationMessage = "";
        }

        public void CancelPanel()
        {
            PanelMode = ClientPanelMode.None;
            ValidationMessage = "";
            StatusText = "Edicio cancelada.";
        }

        public async Task DeleteSelectedAsync()
        {
            if (SelectedClients.Count == 0)
            {
                StatusText = "Selecciona clients abans d'eliminar.";
                return;
            }

            ClientMutationResult? lastResult = null;
            foreach (var selected in SelectedClients.ToList())
            {
                var current = Clients.FirstOrDefault(client => client.Id == selected.Id) ?? selected;
                lastResult = await _clientDataService.DeleteAsync(current, Clients.ToList());
                ApplyMutation(lastResult);
            }

            SelectedClients.Clear();
            RaiseSelectionProperties();
            if (lastResult is not null)
            {
                StatusText = lastResult.Message;
            }
        }

        public void OpenHistory()
        {
            StatusText = SelectedClients.Count == 0
                ? "Selecciona un client per consultar l'historial."
                : $"Historial pendent d'implementar per {SelectedClients[0].NomEmpresa}.";
        }

        private void ApplyMutation(ClientMutationResult result, IReadOnlyList<int>? preserveSelectionIds = null)
        {
            ReplaceClients(result.Clients);
            StatusText = result.Message;
            if (preserveSelectionIds is not null)
            {
                SetSelection(Clients.Where(client => preserveSelectionIds.Contains(client.Id)));
            }

            _ = _shell.RefreshSyncStatusAsync(null, result.PendingCount == 0 ? "Sincronitzat" : result.Message);
        }

        private void ReplaceClients(IReadOnlyList<ClientRecord> clients)
        {
            Clients.Clear();
            foreach (var client in clients.OrderByDescending(client => client.Id < 0).ThenBy(client => client.Id))
            {
                Clients.Add(client);
            }

            RefreshFilter();
            RaiseMetrics();
        }

        private void RefreshFilter()
        {
            var query = SearchText.Trim().ToLowerInvariant();
            var clients = string.IsNullOrWhiteSpace(query)
                ? Clients
                : Clients.Where(client =>
                    client.IdText.Contains(query) ||
                    client.NomEmpresa.ToLowerInvariant().Contains(query) ||
                    (client.NifEmpresa?.ToLowerInvariant().Contains(query) ?? false) ||
                    client.Telefon.ToLowerInvariant().Contains(query) ||
                    (client.Email?.ToLowerInvariant().Contains(query) ?? false) ||
                    client.Poblacio.ToLowerInvariant().Contains(query) ||
                    client.SyncStateText.ToLowerInvariant().Contains(query) ||
                    (client.NomResponsable?.ToLowerInvariant().Contains(query) ?? false));

            FilteredClients.Clear();
            foreach (var client in clients)
            {
                FilteredClients.Add(client);
            }

            SetSelection(SelectedClients.Where(FilteredClients.Contains).ToList());
            RaiseMetrics();
        }

        private void LoadDraft(ClientRecord client)
        {
            ValidationMessage = "";
            DraftNomEmpresa = client.NomEmpresa;
            DraftNifEmpresa = client.NifEmpresa ?? "";
            DraftTelefon = client.Telefon;
            DraftEmail = client.Email ?? "";
            DraftAdreca = client.Adreca;
            DraftPoblacio = client.Poblacio;
            DraftNomResponsable = client.NomResponsable ?? "";
        }

        private bool ValidateDraft()
        {
            var errors = new List<string>();
            Require(DraftNomEmpresa, "Empresa", errors);
            Require(DraftTelefon, "Telefon", errors);
            Require(DraftAdreca, "Adreca", errors);
            Require(DraftPoblacio, "Poblacio", errors);

            MaxLength(DraftNifEmpresa, 20, "NIF empresa", errors);
            MaxLength(DraftTelefon, 25, "Telefon", errors);
            MaxLength(DraftAdreca, 500, "Adreca", errors);
            MaxLength(DraftPoblacio, 100, "Poblacio", errors);
            MaxLength(DraftNomResponsable, 255, "Responsable", errors);

            if (!string.IsNullOrWhiteSpace(DraftNifEmpresa) && !InputValidation.IsValidSpanishTaxId(DraftNifEmpresa))
            {
                errors.Add("NIF empresa ha de tenir format DNI, NIE o CIF, per exemple 12345678Z, X1234567L o B12345678.");
            }

            if (!string.IsNullOrWhiteSpace(DraftTelefon) && !InputValidation.IsValidPhone(DraftTelefon, 25))
            {
                errors.Add("Telefon ha de tenir un format valid, amb 6 a 15 digits i nomes +, espais, punts, guions o parentesis.");
            }

            if (!string.IsNullOrWhiteSpace(DraftEmail) && !InputValidation.IsValidEmail(DraftEmail))
            {
                errors.Add("Correu electronic ha de tenir un format valid.");
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

        private ClientRecord CreateFromDraft(int id)
        {
            return Create(
                id,
                string.IsNullOrWhiteSpace(DraftNomEmpresa) ? "Client sense nom" : DraftNomEmpresa.Trim(),
                EmptyToNull(DraftNifEmpresa),
                string.IsNullOrWhiteSpace(DraftTelefon) ? "-" : DraftTelefon.Trim(),
                EmptyToNull(DraftEmail),
                string.IsNullOrWhiteSpace(DraftAdreca) ? "-" : DraftAdreca.Trim(),
                string.IsNullOrWhiteSpace(DraftPoblacio) ? "-" : DraftPoblacio.Trim(),
                EmptyToNull(DraftNomResponsable),
                ClientSyncState.Synced,
                "Preparat");
        }

        private void SaveDraftInto(ClientRecord client)
        {
            var updated = CreateFromDraft(client.Id);
            client.NomEmpresa = updated.NomEmpresa;
            client.NifEmpresa = updated.NifEmpresa;
            client.Telefon = updated.Telefon;
            client.Email = updated.Email;
            client.Adreca = updated.Adreca;
            client.Poblacio = updated.Poblacio;
            client.NomResponsable = updated.NomResponsable;
        }

        private void RaiseMetrics()
        {
            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(FilteredCount));
            OnPropertyChanged(nameof(WithEmailCount));
            OnPropertyChanged(nameof(WithoutResponsibleCount));
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

        private static ClientRecord Create(
            int id,
            string nomEmpresa,
            string? nifEmpresa,
            string telefon,
            string? email,
            string adreca,
            string poblacio,
            string? nomResponsable,
            ClientSyncState syncState,
            string syncMessage)
        {
            return new ClientRecord
            {
                Id = id,
                NomEmpresa = nomEmpresa,
                NifEmpresa = nifEmpresa,
                Telefon = telefon,
                Email = email,
                Adreca = adreca,
                Poblacio = poblacio,
                NomResponsable = nomResponsable,
                SyncState = syncState,
                SyncMessage = syncMessage
            };
        }

        private static string? EmptyToNull(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
