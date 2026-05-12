using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Threading.Tasks;
using PaletixDesktop.Models;
using PaletixDesktop.Services;
using PaletixDesktop.Settings;

namespace PaletixDesktop.ViewModels
{
    public sealed class ShellViewModel : ViewModelBase
    {
        private readonly AuthService _authService;
        private readonly PermissionService _permissionService;
        private readonly SyncQueue _syncQueue;
        private SessionUser? _currentUser;
        private SyncStatus _syncStatus = new()
        {
            IsOnline = true,
            PendingOperations = 0,
            Message = "Preparat"
        };

        public ShellViewModel(
            AuthService authService,
            PermissionService permissionService,
            SyncQueue syncQueue)
        {
            _authService = authService;
            _permissionService = permissionService;
            _syncQueue = syncQueue;
        }

        public string AppName => AppConstants.AppName;
        public string WindowTitle => "Paletix Desktop";

        public SessionUser? CurrentUser
        {
            get => _currentUser;
            private set
            {
                if (SetProperty(ref _currentUser, value))
                {
                    OnPropertyChanged(nameof(UserDisplayName));
                    OnPropertyChanged(nameof(UserRoleLine));
                    OnPropertyChanged(nameof(UserProgressLine));
                    RaisePermissionProperties();
                }
            }
        }

        public SyncStatus SyncStatus
        {
            get => _syncStatus;
            private set
            {
                if (SetProperty(ref _syncStatus, value))
                {
                    OnPropertyChanged(nameof(SyncStateText));
                    OnPropertyChanged(nameof(SyncDetailText));
                    OnPropertyChanged(nameof(SyncStateBackground));
                    OnPropertyChanged(nameof(HasPendingOperations));
                    OnPropertyChanged(nameof(PendingOperationsText));
                    OnPropertyChanged(nameof(HasStatusAlert));
                    OnPropertyChanged(nameof(StatusAlertSeverity));
                    OnPropertyChanged(nameof(StatusAlertTitle));
                    OnPropertyChanged(nameof(StatusAlertMessage));
                }
            }
        }

        public string UserDisplayName => CurrentUser?.DisplayName ?? "Sessio local";
        public string UserRoleLine => CurrentUser is null ? "Sense usuari" : $"{CurrentUser.RoleName} · {CurrentUser.JobTitle}";
        public string UserProgressLine => CurrentUser is null ? "" : $"Nivell {CurrentUser.Level} · {CurrentUser.Points} punts";
        public string SyncStateText => SyncStatus.IsOnline ? "Online" : "Offline";
        public Brush SyncStateBackground => (Brush)Application.Current.Resources[SyncStatus.IsOnline ? "AppAccentBrush" : "AppDangerBrush"];
        public string SyncDetailText => SyncStatus.PendingOperations == 0
            ? SyncStatus.Message
            : $"{SyncStatus.PendingOperations} canvis pendents";
        public bool HasPendingOperations => SyncStatus.PendingOperations > 0;
        public string PendingOperationsText => SyncStatus.PendingOperations == 0 ? "" : SyncStatus.PendingOperations.ToString();
        public bool HasStatusAlert => SyncStatus.IsOnline && SyncStatus.PendingOperations > 0;
        public InfoBarSeverity StatusAlertSeverity => SyncStatus.PendingOperations > 0
                ? InfoBarSeverity.Informational
                : InfoBarSeverity.Success;
        public string StatusAlertTitle => "Canvis pendents";
        public string StatusAlertMessage => $"{SyncStatus.PendingOperations} canvi(s) pendent(s) de sincronitzar.";

        public bool CanViewOperations => _permissionService.CanAccess(AppFeature.Operations);
        public bool CanViewWarehouse => _permissionService.CanAccess(AppFeature.Warehouse);
        public bool CanViewCatalog => _permissionService.CanAccess(AppFeature.Catalog);
        public bool CanViewClients => _permissionService.CanAccess(AppFeature.Clients);
        public bool CanViewSuppliers => _permissionService.CanAccess(AppFeature.Suppliers);
        public bool CanViewFleet => _permissionService.CanAccess(AppFeature.Fleet);
        public bool CanViewBilling => _permissionService.CanAccess(AppFeature.Billing);
        public bool CanViewGamification => _permissionService.CanAccess(AppFeature.Gamification);
        public bool CanViewUsers => _permissionService.CanAccess(AppFeature.Users);
        public bool CanViewAdministration => _permissionService.CanAccess(AppFeature.Administration);

        public async Task InitializeAsync()
        {
            IsBusy = true;
            ErrorMessage = null;

            try
            {
                var user = await _authService.LoadCurrentUserAsync();
                _permissionService.SetCurrentUser(user);
                CurrentUser = user;

                var pending = await _syncQueue.GetPendingCountAsync();
                SyncStatus = new SyncStatus
                {
                    IsOnline = true,
                    PendingOperations = pending,
                    Message = user.IsLocalSession ? "Sessio local" : "Sincronitzat"
                };
            }
            catch (System.Exception ex)
            {
                SyncStatus = new SyncStatus
                {
                    IsOnline = false,
                    PendingOperations = 0,
                    Message = "Mode offline"
                };
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task RefreshSyncStatusAsync(bool? isOnline = null, string? message = null)
        {
            var pending = await _syncQueue.GetPendingCountAsync();
            SyncStatus = new SyncStatus
            {
                IsOnline = isOnline ?? SyncStatus.IsOnline,
                PendingOperations = pending,
                Message = message ?? (pending == 0 ? "Sincronitzat" : SyncStatus.Message)
            };
        }

        private void RaisePermissionProperties()
        {
            OnPropertyChanged(nameof(CanViewOperations));
            OnPropertyChanged(nameof(CanViewWarehouse));
            OnPropertyChanged(nameof(CanViewCatalog));
            OnPropertyChanged(nameof(CanViewClients));
            OnPropertyChanged(nameof(CanViewSuppliers));
            OnPropertyChanged(nameof(CanViewFleet));
            OnPropertyChanged(nameof(CanViewBilling));
            OnPropertyChanged(nameof(CanViewGamification));
            OnPropertyChanged(nameof(CanViewUsers));
            OnPropertyChanged(nameof(CanViewAdministration));
        }
    }
}
