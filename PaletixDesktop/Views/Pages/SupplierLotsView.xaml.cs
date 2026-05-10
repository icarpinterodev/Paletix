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
    public sealed partial class SupplierLotsView : UserControl, IShellCommandHandler
    {
        private bool _syncingSelection;
        private bool _loaded;

        public SupplierLotsView()
        {
            ViewModel = new SupplierLotCatalogViewModel();
            InitializeComponent();
            DataContext = ViewModel;
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        public SupplierLotCatalogViewModel ViewModel { get; }
        public bool CanHandleShellCommand(string commandId) => commandId.StartsWith("lots.");
        public void HandleShellCommand(string commandId) => _ = HandleShellCommandAsync(commandId);

        public async Task RefreshAsync()
        {
            await ViewModel.LoadAsync();
            SyncSelectionToControls();
        }

        private async Task HandleShellCommandAsync(string commandId)
        {
            switch (commandId)
            {
                case "lots.refresh": await RefreshAsync(); break;
                case "lots.create": ViewModel.CreateRecord(); break;
                case "lots.edit": ViewModel.StartEditSelected(); break;
                case "lots.delete": await DeleteSelectedWithConfirmationAsync(); break;
                case "lots.view.table": ViewModel.SetViewMode(WarehouseCatalogViewMode.Table); break;
                case "lots.view.grid": ViewModel.SetViewMode(WarehouseCatalogViewMode.Grid); break;
            }

            SyncSelectionToControls();
        }

        private async void SupplierLotsView_Loaded(object sender, RoutedEventArgs e)
        {
            if (_loaded) return;
            _loaded = true;
            await RefreshAsync();
        }

        private void RecordsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_syncingSelection) return;
            var source = sender as ListViewBase;
            ViewModel.SetSelection(source?.SelectedItems.OfType<SupplierLotRecord>() ?? Enumerable.Empty<SupplierLotRecord>());
            SyncSelectionToControls(source);
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { Tag: SupplierLotRecord record })
            {
                ViewModel.SelectSingle(record);
                ViewModel.StartEditSelected();
                SyncSelectionToControls();
            }
        }

        private async void SavePanelButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.SavePanelAsync();
            SyncSelectionToControls();
        }

        private void CancelPanelButton_Click(object sender, RoutedEventArgs e) => ViewModel.CancelPanel();
        private void PreviousEditButton_Click(object sender, RoutedEventArgs e) { ViewModel.MoveEdit(-1); SyncSelectionToControls(); }
        private void NextEditButton_Click(object sender, RoutedEventArgs e) { ViewModel.MoveEdit(1); SyncSelectionToControls(); }

        private void RecordCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox { Tag: SupplierLotRecord record }) return;
            var selected = ViewModel.SelectedRecords.ToList();
            if (record.IsSelected)
            {
                if (!selected.Contains(record)) selected.Add(record);
            }
            else
            {
                selected.Remove(record);
            }

            ViewModel.SetSelection(selected);
            SyncSelectionToControls();
        }

        private void HeaderCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (_syncingSelection) return;
            var visibleCount = ViewModel.FilteredRecords.Count;
            var selectedVisibleCount = ViewModel.SelectedRecords.Count(ViewModel.FilteredRecords.Contains);
            var allVisibleSelected = visibleCount > 0 && selectedVisibleCount == visibleCount;
            ViewModel.SetSelection(allVisibleSelected ? Enumerable.Empty<SupplierLotRecord>() : ViewModel.FilteredRecords.ToList());
            SyncSelectionToControls();
        }

        private void Records_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
        {
            var menu = new MenuFlyout();
            if (ViewModel.SelectedCount == 0)
            {
                menu.Items.Add(MenuItem("Registrar lot", "\uE710", () => ViewModel.CreateRecord()));
            }
            else if (ViewModel.SelectedCount == 1)
            {
                menu.Items.Add(MenuItem("Editar lot", "\uE70F", () => ViewModel.StartEditSelected()));
                menu.Items.Add(MenuItem("Eliminar lot", "\uE74D", () => _ = DeleteSelectedWithConfirmationAsync()));
            }
            else
            {
                menu.Items.Add(MenuItem("Editar seleccio", "\uE70F", () => ViewModel.StartEditSelected()));
                menu.Items.Add(MenuItem("Eliminar seleccio", "\uE74D", () => _ = DeleteSelectedWithConfirmationAsync()));
            }

            menu.ShowAt(sender as FrameworkElement);
            args.Handled = true;
        }

        private async Task DeleteSelectedWithConfirmationAsync()
        {
            if (ViewModel.SelectedCount == 0)
            {
                await ViewModel.DeleteSelectedAsync();
                SyncSelectionToControls();
                return;
            }

            var count = ViewModel.SelectedCount;
            var dialog = new ContentDialog
            {
                Title = count == 1 ? "Eliminar lot" : $"Eliminar {count} lots",
                Content = count == 1 ? "Vols eliminar aquest lot?" : $"Vols eliminar aquests {count} lots?",
                PrimaryButtonText = "Eliminar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot
            };

            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
            await ViewModel.DeleteSelectedAsync();
            SyncSelectionToControls();
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(SupplierLotCatalogViewModel.FilteredCount) or nameof(SupplierLotCatalogViewModel.SelectedCount))
            {
                SyncSelectionToControls();
            }
        }

        private static MenuFlyoutItem MenuItem(string text, string glyph, Action action)
        {
            var item = new MenuFlyoutItem { Text = text, Icon = new FontIcon { Glyph = glyph } };
            item.Click += (_, _) => action();
            return item;
        }

        private void SyncSelectionToControls(ListViewBase? source = null)
        {
            _syncingSelection = true;
            try
            {
                if (!ReferenceEquals(source, RecordsTable))
                {
                    RecordsTable.SelectedItems.Clear();
                    foreach (var record in ViewModel.SelectedRecords.Where(ViewModel.FilteredRecords.Contains))
                    {
                        RecordsTable.SelectedItems.Add(record);
                    }
                }

                if (!ReferenceEquals(source, RecordsGrid))
                {
                    RecordsGrid.SelectedItems.Clear();
                    foreach (var record in ViewModel.SelectedRecords.Where(ViewModel.FilteredRecords.Contains))
                    {
                        RecordsGrid.SelectedItems.Add(record);
                    }
                }

                var filteredCount = ViewModel.FilteredRecords.Count;
                var selectedVisibleCount = ViewModel.SelectedRecords.Count(ViewModel.FilteredRecords.Contains);
                SelectVisibleCheckBox.IsChecked = filteredCount > 0 && selectedVisibleCount == filteredCount
                    ? true
                    : selectedVisibleCount == 0 ? false : null;
            }
            finally
            {
                _syncingSelection = false;
            }
        }
    }
}
