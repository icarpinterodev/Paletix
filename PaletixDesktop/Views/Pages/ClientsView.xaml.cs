using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using PaletixDesktop.Models;
using PaletixDesktop.ViewModels;
using PaletixDesktop.Views.Shell;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PaletixDesktop.Views.Pages
{
    public sealed partial class ClientsView : UserControl, IShellCommandHandler
    {
        private bool _syncingSelection;
        private bool _loaded;

        public ClientsView()
        {
            ViewModel = new ClientCatalogViewModel();
            InitializeComponent();
            DataContext = ViewModel;
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        public ClientCatalogViewModel ViewModel { get; }

        public bool CanHandleShellCommand(string commandId)
        {
            return commandId.StartsWith("clients.");
        }

        public void HandleShellCommand(string commandId)
        {
            _ = HandleShellCommandAsync(commandId);
        }

        private async Task HandleShellCommandAsync(string commandId)
        {
            switch (commandId)
            {
                case "clients.refresh":
                    await RefreshAsync();
                    break;
                case "clients.create":
                    ViewModel.CreateClient();
                    break;
                case "clients.edit":
                    ViewModel.StartEditSelected();
                    break;
                case "clients.delete":
                    await DeleteSelectedWithConfirmationAsync();
                    break;
                case "clients.history":
                    ViewModel.OpenHistory();
                    break;
                case "clients.view.table":
                    ViewModel.SetViewMode(ClientCatalogViewMode.Table);
                    break;
                case "clients.view.grid":
                    ViewModel.SetViewMode(ClientCatalogViewMode.Grid);
                    break;
            }

            SyncSelectionToControls();
        }

        private async void ClientsView_Loaded(object sender, RoutedEventArgs e)
        {
            if (_loaded)
            {
                return;
            }

            _loaded = true;
            await RefreshAsync();
            SyncSelectionToControls();
        }

        public async Task RefreshAsync()
        {
            await ViewModel.LoadAsync();
            SyncSelectionToControls();
        }

        private void ClientsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_syncingSelection)
            {
                return;
            }

            var source = sender as ListViewBase;
            ViewModel.SetSelection(source?.SelectedItems.OfType<ClientRecord>() ?? Enumerable.Empty<ClientRecord>());
            SyncSelectionToControls(source);
        }

        private void EditClientButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { Tag: ClientRecord client })
            {
                ViewModel.SelectSingle(client);
                ViewModel.StartEditSelected();
                SyncSelectionToControls();
            }
        }

        private async void DeleteClientButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { Tag: ClientRecord client })
            {
                ViewModel.SelectSingle(client);
                await DeleteSelectedWithConfirmationAsync();
                SyncSelectionToControls();
            }
        }

        private async void SavePanelButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.SavePanelAsync();
            SyncSelectionToControls();
        }

        private void CancelPanelButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.CancelPanel();
        }

        private void PreviousEditButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.MoveEdit(-1);
            SyncSelectionToControls();
        }

        private void NextEditButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.MoveEdit(1);
            SyncSelectionToControls();
        }

        private void ClientCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox { Tag: ClientRecord client })
            {
                return;
            }

            var selected = ViewModel.SelectedClients.ToList();
            if (client.IsSelected)
            {
                if (!selected.Contains(client))
                {
                    selected.Add(client);
                }
            }
            else
            {
                selected.Remove(client);
            }

            ViewModel.SetSelection(selected);
            SyncSelectionToControls();
        }

        private void ClientsHeaderCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (_syncingSelection)
            {
                return;
            }

            var visibleCount = ViewModel.FilteredClients.Count;
            var selectedVisibleCount = ViewModel.SelectedClients.Count(ViewModel.FilteredClients.Contains);
            var allVisibleSelected = visibleCount > 0 && selectedVisibleCount == visibleCount;

            ViewModel.SetSelection(allVisibleSelected
                ? Enumerable.Empty<ClientRecord>()
                : ViewModel.FilteredClients.ToList());

            SyncSelectionToControls();
        }

        private void Clients_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
        {
            var menu = new MenuFlyout();

            if (ViewModel.SelectedCount == 0)
            {
                menu.Items.Add(MenuItem("Nou client", "\uE710", () => ViewModel.CreateClient()));
            }
            else if (ViewModel.SelectedCount == 1)
            {
                menu.Items.Add(MenuItem("Editar client", "\uE70F", () => ViewModel.StartEditSelected()));
                menu.Items.Add(MenuItem("Historial", "\uE81C", () => ViewModel.OpenHistory()));
                menu.Items.Add(MenuItem("Eliminar client", "\uE74D", () => _ = DeleteSelectedFromMenuAsync()));
            }
            else
            {
                menu.Items.Add(MenuItem("Editar seleccio", "\uE70F", () => ViewModel.StartEditSelected()));
                menu.Items.Add(MenuItem("Eliminar seleccio", "\uE74D", () => _ = DeleteSelectedFromMenuAsync()));
            }

            menu.ShowAt(sender as FrameworkElement);
            args.Handled = true;
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(ClientCatalogViewModel.FilteredCount) or nameof(ClientCatalogViewModel.SelectedCount))
            {
                SyncSelectionToControls();
            }
        }

        private static MenuFlyoutItem MenuItem(string text, string glyph, System.Action action)
        {
            var item = new MenuFlyoutItem
            {
                Text = text,
                Icon = new FontIcon { Glyph = glyph }
            };
            item.Click += (_, _) => action();
            return item;
        }

        private async Task DeleteSelectedFromMenuAsync()
        {
            await DeleteSelectedWithConfirmationAsync();
        }

        private async Task DeleteSelectedWithConfirmationAsync()
        {
            if (ViewModel.SelectedCount == 0)
            {
                await ViewModel.DeleteSelectedAsync();
                SyncSelectionToControls();
                return;
            }

            if (!await ConfirmDeleteAsync("client", "clients"))
            {
                return;
            }

            await ViewModel.DeleteSelectedAsync();
            SyncSelectionToControls();
        }

        private async Task<bool> ConfirmDeleteAsync(string singularName, string pluralName)
        {
            var count = ViewModel.SelectedCount;
            var title = count == 1
                ? $"Eliminar {singularName}"
                : $"Eliminar {count} {pluralName}";
            var content = count == 1
                ? $"Vols eliminar aquest {singularName}? Aquesta accio pot quedar pendent si treballes offline."
                : $"Vols eliminar aquests {count} {pluralName}? Aquesta accio pot quedar pendent si treballes offline.";

            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                PrimaryButtonText = "Eliminar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot
            };

            return await dialog.ShowAsync() == ContentDialogResult.Primary;
        }

        private void SyncSelectionToControls(ListViewBase? source = null)
        {
            _syncingSelection = true;
            try
            {
                if (!ReferenceEquals(source, ClientsTable))
                {
                    ClientsTable.SelectedItems.Clear();
                    foreach (var client in ViewModel.SelectedClients.Where(ViewModel.FilteredClients.Contains))
                    {
                        ClientsTable.SelectedItems.Add(client);
                    }
                }

                if (!ReferenceEquals(source, ClientsGrid))
                {
                    ClientsGrid.SelectedItems.Clear();
                    foreach (var client in ViewModel.SelectedClients.Where(ViewModel.FilteredClients.Contains))
                    {
                        ClientsGrid.SelectedItems.Add(client);
                    }
                }

                UpdateHeaderCheckBox();
            }
            finally
            {
                _syncingSelection = false;
            }
        }

        private void UpdateHeaderCheckBox()
        {
            var filteredCount = ViewModel.FilteredClients.Count;
            var selectedVisibleCount = ViewModel.SelectedClients.Count(ViewModel.FilteredClients.Contains);

            SelectVisibleClientsCheckBox.IsChecked = filteredCount > 0 && selectedVisibleCount == filteredCount
                ? true
                : selectedVisibleCount == 0
                    ? false
                    : null;
        }
    }
}
