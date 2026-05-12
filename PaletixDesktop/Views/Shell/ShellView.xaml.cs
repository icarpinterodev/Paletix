using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using PaletixDesktop.Models;
using PaletixDesktop.Settings;
using PaletixDesktop.ViewModels;
using PaletixDesktop.Views.Pages;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace PaletixDesktop.Views.Shell
{
    public sealed partial class ShellView : UserControl
    {
        public event EventHandler? LogoutRequested;

        private bool _initialized;
        private DispatcherTimer? _connectionTimer;
        private bool? _lastOnline;
        private int _connectionFailureCount;
        private bool _checkingConnection;
        private bool _showingConnectionDialog;
        private static readonly TimeSpan[] ReconnectBackoffIntervals =
        {
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(2),
            TimeSpan.FromMinutes(5)
        };

        public ShellView()
        {
            ViewModel = new ShellLayoutViewModel(App.CurrentServices);
            InitializeComponent();
            DataContext = ViewModel;
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
            Unloaded += ShellView_Unloaded;
        }

        public ShellLayoutViewModel ViewModel { get; }

        private async void ShellView_Loaded(object sender, RoutedEventArgs e)
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            await ViewModel.InitializeAsync();
            SyncSelectionControls();
            UpdateRibbon();
            UpdateContent();
            await ViewModel.PendingSync.RefreshAsync();
            StartConnectionMonitor();
        }

        private void ShellView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_connectionTimer is not null)
            {
                _connectionTimer.Stop();
                _connectionTimer.Tick -= ConnectionTimer_Tick;
                _connectionTimer = null;
            }
        }

        private void ShellRoot_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ViewModel.IsCompact = e.NewSize.Width < 880;
            SyncSelectionControls();
        }

        private void CategoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CategoryList.SelectedItem is ShellCategory category)
            {
                ViewModel.SelectCategory(category);
            }
        }

        private void SectionsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SectionsList.SelectedItem is ShellSection section)
            {
                ViewModel.SelectSection(section);
            }
        }

        private void CompactSectionsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CompactSectionsList.SelectedItem is ShellSection section)
            {
                ViewModel.SelectSection(section);
            }
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(ShellLayoutViewModel.ActiveCategory) or nameof(ShellLayoutViewModel.ActiveSection))
            {
                SyncSelectionControls();
            }

            if (e.PropertyName is nameof(ShellLayoutViewModel.ActiveSection))
            {
                UpdateRibbon();
                UpdateContent();
            }

            if (e.PropertyName is nameof(ShellLayoutViewModel.ActiveCommands))
            {
                UpdateRibbon();
            }
        }

        private void SyncSelectionControls()
        {
            CategoryList.SelectedItem = ViewModel.ActiveCategory;
            SectionsList.SelectedItem = ViewModel.ActiveSection;
            CompactSectionsList.SelectedItem = ViewModel.ActiveSection;
        }

        private void UpdateRibbon()
        {
            RibbonBar.PrimaryCommands.Clear();
            RibbonBar.SecondaryCommands.Clear();
            RibbonBar.Content = null;

            foreach (var command in ViewModel.ActiveCommands)
            {
                var button = CreateCommandButton(command);
                if (command.Id == "products.import")
                {
                    RibbonBar.SecondaryCommands.Add(button);
                }
                else
                {
                    RibbonBar.PrimaryCommands.Add(button);
                }
            }

            RibbonBar.SecondaryCommands.Add(new AppBarButton { Label = "Filtrar", Icon = new FontIcon { Glyph = "\uE71C" }, Foreground = PrimaryTextBrush() });
            RibbonBar.SecondaryCommands.Add(new AppBarButton { Label = "Exportar", Icon = new FontIcon { Glyph = "\uE896" }, Foreground = PrimaryTextBrush() });
            RibbonBar.SecondaryCommands.Add(new AppBarButton { Label = "Configuracio", Icon = new FontIcon { Glyph = "\uE713" }, Foreground = PrimaryTextBrush() });
        }

        private void UpdateContent()
        {
            var view = CreateViewForActiveSection();
            if (view is not IShellCommandHandler)
            {
                view.DataContext = ViewModel.ActiveSection;
            }

            ContentHost.Content = view;
        }

        private void ExecuteRibbonCommand(ShellCommand command)
        {
            if (ContentHost.Content is IShellCommandHandler handler && handler.CanHandleShellCommand(command.Id))
            {
                handler.HandleShellCommand(command.Id);
            }

            ViewModel.ExecuteShellCommand(command);
        }

        private AppBarButton CreateCommandButton(ShellCommand command)
        {
            var button = new AppBarButton
            {
                Label = command.Label,
                Icon = new FontIcon { Glyph = command.Glyph },
                Foreground = PrimaryTextBrush()
            };
            button.Click += (_, _) => ExecuteRibbonCommand(command);
            return button;
        }

        private void NotificationsButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Notifications.MarkAllRead();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            LogoutRequested?.Invoke(this, EventArgs.Empty);
        }

        private async void PendingSyncButton_Click(object sender, RoutedEventArgs e)
        {
            await RefreshPendingPanelAsync();
        }

        private void ToggleImportPauseButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ImportJob.TogglePause();
        }

        private void CancelImportButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ImportJob.RequestCancel();
        }

        private async void RefreshPendingButton_Click(object sender, RoutedEventArgs e)
        {
            await RefreshPendingPanelAsync();
        }

        private async void RetryAllPendingButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.PendingSync.RetryAllAsync();
            await RefreshAfterPendingActionAsync("Sincronitzacio reintentada", "S'ha intentat sincronitzar tota la cua pendent.");
        }

        private async void RetryPendingButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { Tag: PendingSyncOperation operation })
            {
                await ViewModel.PendingSync.RetryAsync(operation);
                await RefreshAfterPendingActionAsync("Canvi reintentat", $"S'ha reintentat sincronitzar: {operation.Summary}.");
            }
        }

        private async void DiscardPendingButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement { Tag: PendingSyncOperation operation })
            {
                return;
            }

            if (!await ConfirmDiscardPendingAsync(operation))
            {
                return;
            }

            await ViewModel.PendingSync.DiscardAsync(operation);
            await RefreshAfterPendingActionAsync("Canvi descartat", $"S'ha descartat l'operacio pendent: {operation.Summary}.");
        }

        private void StartConnectionMonitor()
        {
            if (_connectionTimer is not null)
            {
                return;
            }

            _connectionTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(AppConstants.ConnectionCheckIntervalSeconds)
            };
            _connectionTimer.Tick += ConnectionTimer_Tick;
            _connectionTimer.Start();
            _ = CheckConnectionStateAsync();
        }

        private async void ConnectionTimer_Tick(object? sender, object e)
        {
            await CheckConnectionStateAsync();
        }

        private async Task CheckConnectionStateAsync()
        {
            if (_checkingConnection)
            {
                return;
            }

            _checkingConnection = true;
            try
            {
                var online = await App.CurrentServices.ClientDataService.CheckConnectionAsync();
                var wasOnline = _lastOnline;
                _lastOnline = online;

                if (wasOnline != false && !online)
                {
                    await HandleConnectionLostAsync();
                }
                else if (wasOnline == false && online)
                {
                    await HandleConnectionRestoredAsync();
                }
                else
                {
                    await ViewModel.Shell.RefreshSyncStatusAsync(online, online ? "Sincronitzat" : "Mode offline");
                }

                UpdateReconnectBackoff(online);
                await ViewModel.PendingSync.RefreshAsync();
            }
            finally
            {
                _checkingConnection = false;
            }
        }

        private void UpdateReconnectBackoff(bool online)
        {
            if (_connectionTimer is null)
            {
                return;
            }

            if (online)
            {
                _connectionFailureCount = 0;
                _connectionTimer.Interval = TimeSpan.FromSeconds(AppConstants.ConnectionCheckIntervalSeconds);
                return;
            }

            _connectionFailureCount++;
            var index = Math.Min(_connectionFailureCount - 1, ReconnectBackoffIntervals.Length - 1);
            _connectionTimer.Interval = ReconnectBackoffIntervals[index];
        }

        private async Task HandleConnectionLostAsync()
        {
            await ViewModel.Shell.RefreshSyncStatusAsync(false, "Mode offline");
            ViewModel.Notifications.Notify(
                "Connexio perduda",
                "No es pot connectar amb el servidor. L'aplicacio treballa amb SQLite i cua offline.",
                AppNotificationKind.Warning);

            if (ContentHost.Content is ClientsView clientsView)
            {
                await clientsView.RefreshAsync();
            }
            else if (ContentHost.Content is SuppliersView suppliersView)
            {
                await suppliersView.RefreshAsync();
            }
            else if (ContentHost.Content is ProductsView productsView)
            {
                await productsView.RefreshAsync();
            }
            else if (ContentHost.Content is StockView stockView)
            {
                await stockView.RefreshAsync();
            }
            else if (ContentHost.Content is LocationsView locationsView)
            {
                await locationsView.RefreshAsync();
            }
            else if (ContentHost.Content is SupplierLotsView lotsView)
            {
                await lotsView.RefreshAsync();
            }
            else if (ContentHost.Content is PickingView pickingView)
            {
                await pickingView.RefreshAsync();
            }
            else if (ContentHost.Content is OrdersView ordersView)
            {
                await ordersView.RefreshAsync();
            }
            else if (ContentHost.Content is AdministrationUsersView adminUsersView)
            {
                await adminUsersView.RefreshAsync();
            }

            await ShowConnectionLostDialogAsync();
        }

        private async Task HandleConnectionRestoredAsync()
        {
            if (ContentHost.Content is ClientsView clientsView)
            {
                await clientsView.RefreshAsync();
                await App.CurrentServices.SupplierDataService.LoadAsync();
                await App.CurrentServices.ProductDataService.LoadAsync();
            }
            else if (ContentHost.Content is SuppliersView suppliersView)
            {
                await App.CurrentServices.ClientDataService.LoadAsync();
                await App.CurrentServices.ProductDataService.LoadAsync();
                await suppliersView.RefreshAsync();
            }
            else if (ContentHost.Content is ProductsView productsView)
            {
                await App.CurrentServices.ClientDataService.LoadAsync();
                await App.CurrentServices.SupplierDataService.LoadAsync();
                await productsView.RefreshAsync();
            }
            else if (ContentHost.Content is StockView stockView)
            {
                await App.CurrentServices.LocationDataService.LoadAsync();
                await App.CurrentServices.SupplierLotDataService.LoadAsync();
                await stockView.RefreshAsync();
            }
            else if (ContentHost.Content is LocationsView locationsView)
            {
                await App.CurrentServices.StockDataService.LoadAsync();
                await App.CurrentServices.SupplierLotDataService.LoadAsync();
                await locationsView.RefreshAsync();
            }
            else if (ContentHost.Content is SupplierLotsView lotsView)
            {
                await App.CurrentServices.StockDataService.LoadAsync();
                await App.CurrentServices.LocationDataService.LoadAsync();
                await lotsView.RefreshAsync();
            }
            else if (ContentHost.Content is PickingView pickingView)
            {
                await App.CurrentServices.StockDataService.LoadAsync();
                await pickingView.RefreshAsync();
            }
            else if (ContentHost.Content is OrdersView ordersView)
            {
                await ordersView.RefreshAsync();
            }
            else if (ContentHost.Content is AdministrationUsersView adminUsersView)
            {
                await adminUsersView.RefreshAsync();
            }
            else
            {
                var result = await App.CurrentServices.ClientDataService.LoadAsync();
                await App.CurrentServices.SupplierDataService.LoadAsync();
                await App.CurrentServices.ProductDataService.LoadAsync();
                await App.CurrentServices.StockDataService.LoadAsync();
                await App.CurrentServices.LocationDataService.LoadAsync();
                await App.CurrentServices.SupplierLotDataService.LoadAsync();
                await App.CurrentServices.AdminIdentityDataService.LoadAsync();
                await ViewModel.Shell.RefreshSyncStatusAsync(result.IsOnline, result.Message);
            }

            await ViewModel.PendingSync.RefreshAsync();
            ViewModel.Notifications.Notify(
                "Connexio restablerta",
                "La connexio amb el servidor torna a estar disponible. S'ha intentat sincronitzar la cua pendent.",
                AppNotificationKind.Success);
        }

        private async Task ShowConnectionLostDialogAsync()
        {
            if (_showingConnectionDialog || XamlRoot is null)
            {
                return;
            }

            _showingConnectionDialog = true;
            try
            {
                var dialog = new ContentDialog
                {
                    Title = "Connexio perduda",
                    Content = "No es pot connectar amb el servidor. Paletix treballara en mode offline amb SQLite i guardara els canvis pendents per sincronitzar-los quan torni la connexio.",
                    CloseButtonText = "Entes",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = XamlRoot
                };

                await dialog.ShowAsync();
            }
            finally
            {
                _showingConnectionDialog = false;
            }
        }

        private async Task RefreshPendingPanelAsync()
        {
            await ViewModel.PendingSync.RefreshAsync();
            await ViewModel.Shell.RefreshSyncStatusAsync();
        }

        private async Task RefreshAfterPendingActionAsync(string title, string message)
        {
            await RefreshActiveContentAsync();
            await ViewModel.PendingSync.RefreshAsync();
            await ViewModel.Shell.RefreshSyncStatusAsync(null, ViewModel.PendingSync.HasItems ? "Hi ha canvis pendents" : "Sincronitzat");
            ViewModel.Notifications.Notify(title, message, ViewModel.PendingSync.HasItems ? AppNotificationKind.Warning : AppNotificationKind.Success);
        }

        private async Task RefreshActiveContentAsync()
        {
            if (ContentHost.Content is ClientsView clientsView)
            {
                await clientsView.RefreshAsync();
            }
            else if (ContentHost.Content is SuppliersView suppliersView)
            {
                await suppliersView.RefreshAsync();
            }
            else if (ContentHost.Content is ProductsView productsView)
            {
                await productsView.RefreshAsync();
            }
            else if (ContentHost.Content is StockView stockView)
            {
                await stockView.RefreshAsync();
            }
            else if (ContentHost.Content is LocationsView locationsView)
            {
                await locationsView.RefreshAsync();
            }
            else if (ContentHost.Content is SupplierLotsView lotsView)
            {
                await lotsView.RefreshAsync();
            }
            else if (ContentHost.Content is PickingView pickingView)
            {
                await pickingView.RefreshAsync();
            }
            else if (ContentHost.Content is OrdersView ordersView)
            {
                await ordersView.RefreshAsync();
            }
            else if (ContentHost.Content is AdministrationUsersView adminUsersView)
            {
                await adminUsersView.RefreshAsync();
            }
        }

        private async Task<bool> ConfirmDiscardPendingAsync(PendingSyncOperation operation)
        {
            if (XamlRoot is null)
            {
                return true;
            }

            var dialog = new ContentDialog
            {
                Title = "Descartar canvi pendent",
                Content = $"Vols descartar aquesta operacio pendent?\n\n{operation.OperationLabel} · {operation.EntityLabel} · {operation.Summary}",
                PrimaryButtonText = "Descartar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot
            };

            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }

        private FrameworkElement CreateViewForActiveSection()
        {
            if (ViewModel.ActiveSection?.Route == "commercial-products")
            {
                return new ProductsView();
            }

            if (ViewModel.ActiveSection?.Route == "commercial-clients")
            {
                return new ClientsView();
            }

            if (ViewModel.ActiveSection?.Route == "commercial-suppliers")
            {
                return new SuppliersView();
            }

            if (ViewModel.ActiveSection?.Route == "warehouse-stock")
            {
                return new StockView();
            }

            if (ViewModel.ActiveSection?.Route == "warehouse-locations")
            {
                return new LocationsView();
            }

            if (ViewModel.ActiveSection?.Route == "warehouse-lots")
            {
                return new SupplierLotsView();
            }

            if (ViewModel.ActiveSection?.Route == "operations-picking")
            {
                return new PickingView();
            }

            if (ViewModel.ActiveSection?.Route == "operations-orders")
            {
                return new OrdersView();
            }

            if (ViewModel.ActiveSection?.Route == "admin-users")
            {
                return new AdministrationUsersView();
            }

            return ViewModel.ActiveCategory?.Id switch
            {
                "operations" => new OperationsView(),
                "warehouse" => new WarehouseView(),
                "commercial" => new CommercialView(),
                "fleet" => new FleetView(),
                "finance" => new FinanceView(),
                "gamification" => new GamificationView(),
                "administration" => new AdministrationView(),
                _ => new HomeView()
            };
        }

        private static Brush PrimaryTextBrush()
        {
            return (Brush)Application.Current.Resources["AppPrimaryTextBrush"];
        }
    }
}
