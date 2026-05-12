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
    public sealed partial class StockView : UserControl, IShellCommandHandler
    {
        private bool _syncingSelection;
        private bool _loaded;

        public StockView()
        {
            ViewModel = new StockCatalogViewModel();
            InitializeComponent();
            DataContext = ViewModel;
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        public StockCatalogViewModel ViewModel { get; }

        public bool CanHandleShellCommand(string commandId) => commandId.StartsWith("stock.");

        public void HandleShellCommand(string commandId)
        {
            _ = HandleShellCommandAsync(commandId);
        }

        public async Task RefreshAsync()
        {
            await ViewModel.LoadAsync();
            SyncSelectionToControls();
        }

        private async Task HandleShellCommandAsync(string commandId)
        {
            switch (commandId)
            {
                case "stock.refresh":
                    await RefreshAsync();
                    break;
                case "stock.create":
                    ViewModel.CreateRecord();
                    break;
                case "stock.entry":
                    ViewModel.OpenEntrada();
                    break;
                case "stock.move":
                    ViewModel.OpenMoviment();
                    break;
                case "stock.adjust":
                    ViewModel.OpenAjust();
                    break;
                case "stock.reserve":
                    ViewModel.OpenReserva();
                    break;
                case "stock.release":
                    ViewModel.OpenAlliberament();
                    break;
                case "stock.history":
                    await ViewModel.OpenHistoryAsync();
                    break;
                case "stock.edit":
                    ViewModel.StartEditSelected();
                    break;
                case "stock.delete":
                    await DeleteSelectedWithConfirmationAsync();
                    break;
                case "stock.view.table":
                    ViewModel.SetViewMode(WarehouseCatalogViewMode.Table);
                    break;
                case "stock.view.grid":
                    ViewModel.SetViewMode(WarehouseCatalogViewMode.Grid);
                    break;
            }

            SyncSelectionToControls();
        }

        private async void StockView_Loaded(object sender, RoutedEventArgs e)
        {
            if (_loaded)
            {
                return;
            }

            _loaded = true;
            await RefreshAsync();
        }

        private void RecordsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_syncingSelection)
            {
                return;
            }

            var source = sender as ListViewBase;
            ViewModel.SetSelection(source?.SelectedItems.OfType<StockRecord>() ?? Enumerable.Empty<StockRecord>());
            SyncSelectionToControls(source);
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { Tag: StockRecord record })
            {
                ViewModel.SelectSingle(record);
                ViewModel.OpenAjust();
                SyncSelectionToControls();
            }
        }

        private async void SavePanelButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.SavePanelAsync();
            SyncSelectionToControls();
        }

        private void CancelPanelButton_Click(object sender, RoutedEventArgs e) => ViewModel.CancelPanel();
        private async void SaveOperationButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.SaveOperationAsync();
            SyncSelectionToControls();
        }

        private void CancelOperationButton_Click(object sender, RoutedEventArgs e) => ViewModel.CancelOperationPanel();
        private void CloseHistoryButton_Click(object sender, RoutedEventArgs e) => ViewModel.CloseHistory();
        private void OpenLocationPickerButton_Click(object sender, RoutedEventArgs e) => ViewModel.OpenLocationPicker();
        private void CancelLocationPickerButton_Click(object sender, RoutedEventArgs e) => ViewModel.CancelLocationPicker();
        private void PreviousEditButton_Click(object sender, RoutedEventArgs e) { ViewModel.MoveEdit(-1); SyncSelectionToControls(); }
        private void NextEditButton_Click(object sender, RoutedEventArgs e) { ViewModel.MoveEdit(1); SyncSelectionToControls(); }

        private async void LocationPickerCell_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement { Tag: LocationDesignerCell cell }) return;
            if (cell.IsOccupied && !cell.IsCurrentSelection)
            {
                var dialog = new ContentDialog
                {
                    Title = "Ubicacio ocupada",
                    Content = ViewModel.GetOccupiedLocationDialogText(cell),
                    PrimaryButtonText = ViewModel.OccupiedLocationPrimaryButtonText,
                    CloseButtonText = "Triar una altra",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = XamlRoot
                };

                if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                {
                    return;
                }
            }

            ViewModel.SelectLocationFromPicker(cell);
        }

        private void RecordCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox { Tag: StockRecord record })
            {
                return;
            }

            var selected = ViewModel.SelectedRecords.ToList();
            if (record.IsSelected)
            {
                if (!selected.Contains(record))
                {
                    selected.Add(record);
                }
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
            if (_syncingSelection)
            {
                return;
            }

            var visibleCount = ViewModel.FilteredRecords.Count;
            var selectedVisibleCount = ViewModel.SelectedRecords.Count(ViewModel.FilteredRecords.Contains);
            var allVisibleSelected = visibleCount > 0 && selectedVisibleCount == visibleCount;
            ViewModel.SetSelection(allVisibleSelected ? Enumerable.Empty<StockRecord>() : ViewModel.FilteredRecords.ToList());
            SyncSelectionToControls();
        }

        private void Records_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
        {
            var menu = new MenuFlyout();
            if (ViewModel.SelectedCount == 0)
            {
                menu.Items.Add(MenuItem("Entrada de stock", "\uE710", () => ViewModel.OpenEntrada()));
            }
            else if (ViewModel.SelectedCount == 1)
            {
                menu.Items.Add(MenuItem("Moure stock", "\uE8AB", () => ViewModel.OpenMoviment()));
                menu.Items.Add(MenuItem("Ajust inventari", "\uE7C9", () => ViewModel.OpenAjust()));
                menu.Items.Add(MenuItem("Reservar", "\uE72E", () => ViewModel.OpenReserva()));
                menu.Items.Add(MenuItem("Alliberar", "\uE785", () => ViewModel.OpenAlliberament()));
            }
            else
            {
                menu.Items.Add(MenuItem("Moure seleccio", "\uE8AB", () => ViewModel.OpenMoviment()));
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
                Title = count == 1 ? "Eliminar stock" : $"Eliminar {count} registres de stock",
                Content = count == 1 ? "Vols eliminar aquest registre de stock?" : $"Vols eliminar aquests {count} registres de stock?",
                PrimaryButtonText = "Eliminar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot
            };

            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
            {
                return;
            }

            await ViewModel.DeleteSelectedAsync();
            SyncSelectionToControls();
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(StockCatalogViewModel.FilteredCount) or nameof(StockCatalogViewModel.SelectedCount))
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
