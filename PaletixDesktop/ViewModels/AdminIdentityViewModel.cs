using PaletixDesktop.Models;
using PaletixDesktop.Services;
using PaletixDesktop.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace PaletixDesktop.ViewModels
{
    public enum AdminIdentityTab
    {
        Users,
        Roles,
        JobTitles
    }

    public enum AdminIdentityPanelMode
    {
        None,
        Create,
        Edit
    }

    public sealed class AdminIdentityViewModel : ViewModelBase
    {
        private readonly AdminIdentityDataService _dataService;
        private readonly ShellViewModel _shell;
        private AdminIdentityTab _activeTab;
        private AdminIdentityPanelMode _panelMode;
        private string _searchText = "";
        private string _statusText = "Carregant administracio...";
        private string _validationMessage = "";
        private AdminUserRecord _userDraft = NewUserDraft();
        private AdminSimpleRecord _simpleDraft = new();
        private DateTimeOffset _birthDate = DateTimeOffset.Now.AddYears(-18);
        private DateTimeOffset _contractDate = DateTimeOffset.Now;

        public AdminIdentityViewModel()
            : this(App.CurrentServices.AdminIdentityDataService, App.CurrentServices.ShellViewModel)
        {
        }

        public AdminIdentityViewModel(AdminIdentityDataService dataService, ShellViewModel shell)
        {
            _dataService = dataService;
            _shell = shell;
        }

        public ObservableCollection<AdminUserRecord> Users { get; } = new();
        public ObservableCollection<AdminUserRecord> FilteredUsers { get; } = new();
        public ObservableCollection<AdminUserRecord> SelectedUsers { get; } = new();
        public ObservableCollection<AdminSimpleRecord> Roles { get; } = new();
        public ObservableCollection<AdminSimpleRecord> FilteredRoles { get; } = new();
        public ObservableCollection<AdminSimpleRecord> SelectedRoles { get; } = new();
        public ObservableCollection<AdminSimpleRecord> JobTitles { get; } = new();
        public ObservableCollection<AdminSimpleRecord> FilteredJobTitles { get; } = new();
        public ObservableCollection<AdminSimpleRecord> SelectedJobTitles { get; } = new();
        public ObservableCollection<LookupOption> RoleOptions { get; } = new();
        public ObservableCollection<LookupOption> JobTitleOptions { get; } = new();

        public AdminIdentityTab ActiveTab
        {
            get => _activeTab;
            private set
            {
                if (SetProperty(ref _activeTab, value))
                {
                    ClosePanel();
                    RefreshFilter();
                    RaiseTabProperties();
                }
            }
        }

        public AdminIdentityPanelMode PanelMode
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

        public bool IsUsersTab => ActiveTab == AdminIdentityTab.Users;
        public bool IsRolesTab => ActiveTab == AdminIdentityTab.Roles;
        public bool IsJobTitlesTab => ActiveTab == AdminIdentityTab.JobTitles;
        public bool IsPanelOpen => PanelMode != AdminIdentityPanelMode.None;
        public bool IsEditing => PanelMode == AdminIdentityPanelMode.Edit;

        public string PanelTitle => ActiveTab switch
        {
            AdminIdentityTab.Users => PanelMode == AdminIdentityPanelMode.Edit ? "Editar usuari" : "Nou usuari",
            AdminIdentityTab.Roles => PanelMode == AdminIdentityPanelMode.Edit ? "Editar rol" : "Nou rol",
            _ => PanelMode == AdminIdentityPanelMode.Edit ? "Editar carrec" : "Nou carrec"
        };

        public string PanelSubtitle => ActiveTab switch
        {
            AdminIdentityTab.Users => "Fitxa, rol i carrec del treballador.",
            AdminIdentityTab.Roles => "Perfil de permisos base. Els permisos granulars es configuraran en el seguent increment.",
            _ => "Carrec organitzatiu assignable als usuaris."
        };

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

        public AdminUserRecord UserDraft
        {
            get => _userDraft;
            private set
            {
                if (SetProperty(ref _userDraft, value))
                {
                    OnPropertyChanged(nameof(BirthDate));
                    OnPropertyChanged(nameof(ContractDate));
                    RaiseDraftNumberProperties();
                }
            }
        }

        public AdminSimpleRecord SimpleDraft
        {
            get => _simpleDraft;
            private set => SetProperty(ref _simpleDraft, value);
        }

        public DateTimeOffset BirthDate
        {
            get => _birthDate;
            set
            {
                if (SetProperty(ref _birthDate, value))
                {
                    UserDraft.DataNaixement = value.DateTime;
                    ValidateDraft();
                }
            }
        }

        public DateTimeOffset ContractDate
        {
            get => _contractDate;
            set
            {
                if (SetProperty(ref _contractDate, value))
                {
                    UserDraft.DataContractacio = value.DateTime;
                    ValidateDraft();
                }
            }
        }

        public double DraftSalariValue
        {
            get => (double)UserDraft.Salari;
            set
            {
                UserDraft.Salari = (decimal)Math.Max(0, value);
                ValidateDraft();
                OnPropertyChanged();
            }
        }

        public double DraftTornValue
        {
            get => UserDraft.Torn ?? 0;
            set
            {
                UserDraft.Torn = value <= 0 ? null : (sbyte)Math.Min(sbyte.MaxValue, Math.Round(value));
                ValidateDraft();
                OnPropertyChanged();
            }
        }

        public double DraftSaldoPuntsValue
        {
            get => UserDraft.SaldoPunts;
            set
            {
                UserDraft.SaldoPunts = (int)Math.Max(0, Math.Round(value));
                ValidateDraft();
                OnPropertyChanged();
            }
        }

        public double DraftNivellValue
        {
            get => UserDraft.Nivell;
            set
            {
                UserDraft.Nivell = (int)Math.Max(1, Math.Round(value));
                ValidateDraft();
                OnPropertyChanged();
            }
        }

        public double DraftAnysExperienciaValue
        {
            get => UserDraft.AnysExperiencia ?? 0;
            set
            {
                UserDraft.AnysExperiencia = value <= 0 ? null : (sbyte)Math.Min(sbyte.MaxValue, Math.Round(value));
                ValidateDraft();
                OnPropertyChanged();
            }
        }

        public int TotalUsers => Users.Count;
        public int TotalRoles => Roles.Count;
        public int TotalJobTitles => JobTitles.Count;
        public int PendingUsers => Users.Count(item => item.IsPending);
        public int PendingRoles => Roles.Count(item => item.IsPending);
        public int PendingJobTitles => JobTitles.Count(item => item.IsPending);
        public int ActivePendingCount => ActiveTab switch
        {
            AdminIdentityTab.Users => PendingUsers,
            AdminIdentityTab.Roles => PendingRoles,
            _ => PendingJobTitles
        };
        public int FilteredCount => ActiveTab switch
        {
            AdminIdentityTab.Users => FilteredUsers.Count,
            AdminIdentityTab.Roles => FilteredRoles.Count,
            _ => FilteredJobTitles.Count
        };
        public int SelectedCount => ActiveTab switch
        {
            AdminIdentityTab.Users => SelectedUsers.Count,
            AdminIdentityTab.Roles => SelectedRoles.Count,
            _ => SelectedJobTitles.Count
        };
        public string SelectedCountText => $"{SelectedCount} seleccionat(s)";

        public async Task LoadAsync()
        {
            IsBusy = true;
            ErrorMessage = null;
            try
            {
                var result = await _dataService.LoadAsync();
                Replace(Users, result.Users);
                Replace(Roles, result.Roles);
                Replace(JobTitles, result.JobTitles);
                RefreshOptions();
                RefreshFilter();
                StatusText = result.Message;
                await _shell.RefreshSyncStatusAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                StatusText = $"No s'ha pogut carregar administracio: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void SetActiveTab(AdminIdentityTab tab)
        {
            ActiveTab = tab;
        }

        public void SetUserSelection(IEnumerable<AdminUserRecord> users)
        {
            ApplySelection(Users, SelectedUsers, users);
            RaiseSelectionProperties();
        }

        public void SetRoleSelection(IEnumerable<AdminSimpleRecord> roles)
        {
            ApplySelection(Roles, SelectedRoles, roles);
            RaiseSelectionProperties();
        }

        public void SetJobTitleSelection(IEnumerable<AdminSimpleRecord> jobTitles)
        {
            ApplySelection(JobTitles, SelectedJobTitles, jobTitles);
            RaiseSelectionProperties();
        }

        public void OpenCreate()
        {
            ValidationMessage = "";
            PanelMode = AdminIdentityPanelMode.Create;
            if (ActiveTab == AdminIdentityTab.Users)
            {
                UserDraft = NewUserDraft();
                UserDraft.IdRol = RoleOptions.FirstOrDefault(option => option.Id is not null)?.Id ?? 0;
                UserDraft.IdCarrec = JobTitleOptions.FirstOrDefault(option => option.Id is not null)?.Id ?? 0;
                BirthDate = new DateTimeOffset(UserDraft.DataNaixement);
                ContractDate = new DateTimeOffset(UserDraft.DataContractacio);
                UserDraft.PropertyChanged += (_, _) => ValidateDraft();
            }
            else
            {
                SimpleDraft = new AdminSimpleRecord();
                SimpleDraft.PropertyChanged += (_, _) => ValidateDraft();
            }
        }

        public void OpenEditSelected()
        {
            ValidationMessage = "";
            PanelMode = AdminIdentityPanelMode.Edit;
            if (ActiveTab == AdminIdentityTab.Users && SelectedUsers.FirstOrDefault() is { } user)
            {
                UserDraft = CloneUser(user);
                UserDraft.Password = "";
                BirthDate = new DateTimeOffset(UserDraft.DataNaixement);
                ContractDate = new DateTimeOffset(UserDraft.DataContractacio);
                UserDraft.PropertyChanged += (_, _) => ValidateDraft();
            }
            else if (ActiveTab == AdminIdentityTab.Roles && SelectedRoles.FirstOrDefault() is { } role)
            {
                SimpleDraft = CloneSimple(role);
                SimpleDraft.PropertyChanged += (_, _) => ValidateDraft();
            }
            else if (ActiveTab == AdminIdentityTab.JobTitles && SelectedJobTitles.FirstOrDefault() is { } jobTitle)
            {
                SimpleDraft = CloneSimple(jobTitle);
                SimpleDraft.PropertyChanged += (_, _) => ValidateDraft();
            }
            else
            {
                PanelMode = AdminIdentityPanelMode.None;
            }
        }

        public async Task SaveAsync()
        {
            if (!ValidateDraft())
            {
                return;
            }

            IsBusy = true;
            try
            {
                if (ActiveTab == AdminIdentityTab.Users)
                {
                    var result = PanelMode == AdminIdentityPanelMode.Edit
                        ? await _dataService.UpdateUserAsync(UserDraft, Users)
                        : await _dataService.CreateUserAsync(UserDraft, Users);
                    Replace(Users, result.Users);
                    SetUserSelection(result.User is null ? Enumerable.Empty<AdminUserRecord>() : new[] { result.User });
                    StatusText = result.Message;
                }
                else if (ActiveTab == AdminIdentityTab.Roles)
                {
                    var result = PanelMode == AdminIdentityPanelMode.Edit
                        ? await _dataService.UpdateRoleAsync(SimpleDraft, Roles)
                        : await _dataService.CreateRoleAsync(SimpleDraft, Roles);
                    Replace(Roles, result.Records);
                    SetRoleSelection(result.Record is null ? Enumerable.Empty<AdminSimpleRecord>() : new[] { result.Record });
                    StatusText = result.Message;
                }
                else
                {
                    var result = PanelMode == AdminIdentityPanelMode.Edit
                        ? await _dataService.UpdateJobTitleAsync(SimpleDraft, JobTitles)
                        : await _dataService.CreateJobTitleAsync(SimpleDraft, JobTitles);
                    Replace(JobTitles, result.Records);
                    SetJobTitleSelection(result.Record is null ? Enumerable.Empty<AdminSimpleRecord>() : new[] { result.Record });
                    StatusText = result.Message;
                }

                RefreshOptions();
                RefreshFilter();
                ClosePanel();
                await _shell.RefreshSyncStatusAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeleteSelectedAsync()
        {
            IsBusy = true;
            try
            {
                if (ActiveTab == AdminIdentityTab.Users)
                {
                    foreach (var user in SelectedUsers.ToList())
                    {
                        var result = await _dataService.DeleteUserAsync(user, Users);
                        Replace(Users, result.Users);
                        StatusText = result.Message;
                    }
                    SetUserSelection(Enumerable.Empty<AdminUserRecord>());
                }
                else if (ActiveTab == AdminIdentityTab.Roles)
                {
                    foreach (var role in SelectedRoles.ToList())
                    {
                        var result = await _dataService.DeleteRoleAsync(role, Roles);
                        Replace(Roles, result.Records);
                        StatusText = result.Message;
                    }
                    SetRoleSelection(Enumerable.Empty<AdminSimpleRecord>());
                }
                else
                {
                    foreach (var jobTitle in SelectedJobTitles.ToList())
                    {
                        var result = await _dataService.DeleteJobTitleAsync(jobTitle, JobTitles);
                        Replace(JobTitles, result.Records);
                        StatusText = result.Message;
                    }
                    SetJobTitleSelection(Enumerable.Empty<AdminSimpleRecord>());
                }

                RefreshOptions();
                RefreshFilter();
                await _shell.RefreshSyncStatusAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void ClosePanel()
        {
            PanelMode = AdminIdentityPanelMode.None;
            ValidationMessage = "";
        }

        private bool ValidateDraft()
        {
            if (!IsPanelOpen)
            {
                return true;
            }

            if (ActiveTab == AdminIdentityTab.Users)
            {
                var errors = new List<string>();
                if (string.IsNullOrWhiteSpace(UserDraft.Nom)) errors.Add("El nom es obligatori.");
                if (string.IsNullOrWhiteSpace(UserDraft.Cognoms)) errors.Add("Els cognoms son obligatoris.");
                if (!InputValidation.IsValidSpanishTaxIdFormat(UserDraft.Dni)) errors.Add("El DNI/NIE/CIF no te un format valid.");
                if (!InputValidation.IsValidEmail(UserDraft.Email)) errors.Add("El correu electronic no es valid.");
                if (!InputValidation.IsValidPhone(UserDraft.Telefon, 25)) errors.Add("El telefon no es valid.");
                if (UserDraft.DataNaixement.Date > DateTime.Today.AddYears(-16)) errors.Add("La data de naixement ha de correspondre a una edat laboral valida.");
                if (UserDraft.DataContractacio.Date > DateTime.Today.AddDays(1)) errors.Add("La data de contractacio no pot ser futura.");
                if (UserDraft.IdRol <= 0) errors.Add("Selecciona un rol.");
                if (UserDraft.IdCarrec <= 0) errors.Add("Selecciona un carrec.");
                if (UserDraft.Nivell < 1) errors.Add("El nivell ha de ser com a minim 1.");
                if (UserDraft.Salari < 0) errors.Add("El salari no pot ser negatiu.");
                if (PanelMode != AdminIdentityPanelMode.Edit && string.IsNullOrWhiteSpace(UserDraft.Password)) errors.Add("La contrasenya es obligatoria per crear un usuari.");
                if (!string.IsNullOrWhiteSpace(UserDraft.Password) && UserDraft.Password.Length < 8) errors.Add("La contrasenya ha de tenir com a minim 8 caracters.");

                ValidationMessage = string.Join(Environment.NewLine, errors);
                return errors.Count == 0;
            }

            ValidationMessage = string.IsNullOrWhiteSpace(SimpleDraft.Nom)
                ? "El nom es obligatori."
                : "";
            return !HasValidationErrors;
        }

        private void RefreshFilter()
        {
            var query = SearchText.Trim();
            Replace(FilteredUsers, Users.Where(user => UserMatches(user, query)));
            Replace(FilteredRoles, Roles.Where(record => SimpleMatches(record, query)));
            Replace(FilteredJobTitles, JobTitles.Where(record => SimpleMatches(record, query)));
            RaiseMetrics();
        }

        private void RefreshOptions()
        {
            Replace(RoleOptions, Roles.Where(role => role.Id > 0 && !role.IsPendingDelete).Select(role => new LookupOption { Id = role.Id, Label = role.Nom }));
            Replace(JobTitleOptions, JobTitles.Where(jobTitle => jobTitle.Id > 0 && !jobTitle.IsPendingDelete).Select(jobTitle => new LookupOption { Id = jobTitle.Id, Label = jobTitle.Nom }));
            foreach (var user in Users)
            {
                user.RolText = Roles.FirstOrDefault(role => role.Id == user.IdRol)?.Nom ?? $"Rol {user.IdRol}";
                user.CarrecText = JobTitles.FirstOrDefault(jobTitle => jobTitle.Id == user.IdCarrec)?.Nom ?? $"Carrec {user.IdCarrec}";
            }
        }

        private static bool UserMatches(AdminUserRecord user, string query)
        {
            if (query.Length == 0)
            {
                return true;
            }

            return Contains(user.IdText, query)
                || Contains(user.Nom, query)
                || Contains(user.Cognoms, query)
                || Contains(user.Dni, query)
                || Contains(user.Email, query)
                || Contains(user.Telefon, query)
                || Contains(user.RolText, query)
                || Contains(user.CarrecText, query)
                || Contains(user.SyncStateText, query);
        }

        private static bool SimpleMatches(AdminSimpleRecord record, string query)
        {
            return query.Length == 0
                || Contains(record.IdText, query)
                || Contains(record.Nom, query)
                || Contains(record.SyncStateText, query);
        }

        private static bool Contains(string? value, string query) =>
            value?.Contains(query, StringComparison.CurrentCultureIgnoreCase) == true;

        private static AdminUserRecord NewUserDraft() => new()
        {
            DataNaixement = DateTime.Today.AddYears(-18),
            DataContractacio = DateTime.Today,
            Nivell = 1,
            SaldoPunts = 0,
            Salari = 0
        };

        private static AdminUserRecord CloneUser(AdminUserRecord user) => new()
        {
            Id = user.Id,
            Nom = user.Nom,
            Cognoms = user.Cognoms,
            Dni = user.Dni,
            DataNaixement = user.DataNaixement,
            DataContractacio = user.DataContractacio,
            Email = user.Email,
            Telefon = user.Telefon,
            Password = user.Password,
            Salari = user.Salari,
            Torn = user.Torn,
            NumSeguretatSocial = user.NumSeguretatSocial,
            NumCompteBancari = user.NumCompteBancari,
            IdCarrec = user.IdCarrec,
            IdRol = user.IdRol,
            SaldoPunts = user.SaldoPunts,
            Nivell = user.Nivell,
            AnysExperiencia = user.AnysExperiencia,
            DataDeCreacio = user.DataDeCreacio,
            RolText = user.RolText,
            CarrecText = user.CarrecText,
            SyncState = user.SyncState,
            SyncMessage = user.SyncMessage
        };

        private static AdminSimpleRecord CloneSimple(AdminSimpleRecord record) => new()
        {
            Id = record.Id,
            Nom = record.Nom,
            SyncState = record.SyncState,
            SyncMessage = record.SyncMessage
        };

        private static void ApplySelection<T>(IEnumerable<T> all, ObservableCollection<T> selectedCollection, IEnumerable<T> selected)
            where T : AdminIdentityRecordBase
        {
            var selectedList = selected.ToList();
            selectedCollection.Clear();
            foreach (var item in all)
            {
                item.IsSelected = selectedList.Contains(item);
                if (item.IsSelected)
                {
                    selectedCollection.Add(item);
                }
            }
        }

        private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> items)
        {
            target.Clear();
            foreach (var item in items)
            {
                target.Add(item);
            }
        }

        private void RaiseTabProperties()
        {
            OnPropertyChanged(nameof(IsUsersTab));
            OnPropertyChanged(nameof(IsRolesTab));
            OnPropertyChanged(nameof(IsJobTitlesTab));
            OnPropertyChanged(nameof(PanelTitle));
            OnPropertyChanged(nameof(PanelSubtitle));
            OnPropertyChanged(nameof(ActivePendingCount));
        }

        private void RaiseSelectionProperties()
        {
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(SelectedCountText));
        }

        private void RaiseDraftNumberProperties()
        {
            OnPropertyChanged(nameof(DraftSalariValue));
            OnPropertyChanged(nameof(DraftTornValue));
            OnPropertyChanged(nameof(DraftSaldoPuntsValue));
            OnPropertyChanged(nameof(DraftNivellValue));
            OnPropertyChanged(nameof(DraftAnysExperienciaValue));
        }

        private void RaiseMetrics()
        {
            OnPropertyChanged(nameof(TotalUsers));
            OnPropertyChanged(nameof(TotalRoles));
            OnPropertyChanged(nameof(TotalJobTitles));
            OnPropertyChanged(nameof(PendingUsers));
            OnPropertyChanged(nameof(PendingRoles));
            OnPropertyChanged(nameof(PendingJobTitles));
            OnPropertyChanged(nameof(ActivePendingCount));
            OnPropertyChanged(nameof(FilteredCount));
            RaiseSelectionProperties();
        }
    }
}
