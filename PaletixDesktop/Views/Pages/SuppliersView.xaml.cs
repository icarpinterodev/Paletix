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
    public sealed partial class SuppliersView : UserControl, IShellCommandHandler
    {
        private bool _syncingSelection;
        private bool _loaded;

        public SuppliersView()
        {
            ViewModel = new SupplierCatalogViewModel();
            InitializeComponent();
            DataContext = ViewModel;
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        public SupplierCatalogViewModel ViewModel { get; }

        public bool CanHandleShellCommand(string commandId)
        {
            return commandId.StartsWith("suppliers.");
        }

        public void HandleShellCommand(string commandId)
        {
            _ = HandleShellCommandAsync(commandId);
        }

        private async Task HandleShellCommandAsync(string commandId)
        {
            switch (commandId)
            {
                case "suppliers.refresh":
                    await RefreshAsync();
                    break;
                case "suppliers.create":
                    ViewModel.CreateSupplier();
                    break;
                case "suppliers.edit":
                    ViewModel.StartEditSelected();
                    break;
                case "suppliers.delete":
                    await DeleteSelectedWithConfirmationAsync();
                    break;
                case "suppliers.compare":
                    ViewModel.CompareSelected();
                    break;
                case "suppliers.view.table":
                    ViewModel.SetViewMode(SupplierCatalogViewMode.Table);
                    break;
                case "suppliers.view.grid":
                    ViewModel.SetViewMode(SupplierCatalogViewMode.Grid);
                    break;
            }

            SyncSelectionToControls();
        }

        private async void SuppliersView_Loaded(object sender, RoutedEventArgs e)
        {
            if (_loaded)
            {
                return;
            }

            _loaded = true;
            await RefreshAsync();
        }

        public async Task RefreshAsync()
        {
            await ViewModel.LoadAsync();
            SyncSelectionToControls();
        }

        private void SuppliersSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_syncingSelection)
            {
                return;
            }

            var source = sender as ListViewBase;
            ViewModel.SetSelection(source?.SelectedItems.OfType<SupplierRecord>() ?? Enumerable.Empty<SupplierRecord>());
            SyncSelectionToControls(source);
        }

        private void EditSupplierButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { Tag: SupplierRecord supplier })
            {
                ViewModel.SelectSingle(supplier);
                ViewModel.StartEditSelected();
                SyncSelectionToControls();
            }
        }

        private async void DeleteSupplierButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { Tag: SupplierRecord supplier })
            {
                ViewModel.SelectSingle(supplier);
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

        private void SupplierCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox { Tag: SupplierRecord supplier })
            {
                return;
            }

            var selected = ViewModel.SelectedSuppliers.ToList();
            if (supplier.IsSelected)
            {
                if (!selected.Contains(supplier))
                {
                    selected.Add(supplier);
                }
            }
            else
            {
                selected.Remove(supplier);
            }

            ViewModel.SetSelection(selected);
            SyncSelectionToControls();
        }

        private void SuppliersHeaderCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (_syncingSelection)
            {
                return;
            }

            var visibleCount = ViewModel.FilteredSuppliers.Count;
            var selectedVisibleCount = ViewModel.SelectedSuppliers.Count(ViewModel.FilteredSuppliers.Contains);
            var allVisibleSelected = visibleCount > 0 && selectedVisibleCount == visibleCount;

            ViewModel.SetSelection(allVisibleSelected
                ? Enumerable.Empty<SupplierRecord>()
                : ViewModel.FilteredSuppliers.ToList());

            SyncSelectionToControls();
        }

        private void Suppliers_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
        {
            var menu = new MenuFlyout();

            if (ViewModel.SelectedCount == 0)
            {
                menu.Items.Add(MenuItem("Nou proveidor", "\uE710", () => ViewModel.CreateSupplier()));
            }
            else if (ViewModel.SelectedCount == 1)
            {
                menu.Items.Add(MenuItem("Editar proveidor", "\uE70F", () => ViewModel.StartEditSelected()));
                menu.Items.Add(MenuItem("Comparar", "\uE9D2", () => ViewModel.CompareSelected()));
                menu.Items.Add(MenuItem("Eliminar proveidor", "\uE74D", () => _ = DeleteSelectedFromMenuAsync()));
            }
            else
            {
                menu.Items.Add(MenuItem("Editar seleccio", "\uE70F", () => ViewModel.StartEditSelected()));
                menu.Items.Add(MenuItem("Comparar seleccio", "\uE9D2", () => ViewModel.CompareSelected()));
                menu.Items.Add(MenuItem("Eliminar seleccio", "\uE74D", () => _ = DeleteSelectedFromMenuAsync()));
            }

            menu.ShowAt(sender as FrameworkElement);
            args.Handled = true;
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(SupplierCatalogViewModel.FilteredCount) or nameof(SupplierCatalogViewModel.SelectedCount))
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

            if (!await ConfirmDeleteAsync("proveidor", "proveidors"))
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
                if (!ReferenceEquals(source, SuppliersTable))
                {
                    SuppliersTable.SelectedItems.Clear();
                    foreach (var supplier in ViewModel.SelectedSuppliers.Where(ViewModel.FilteredSuppliers.Contains))
                    {
                        SuppliersTable.SelectedItems.Add(supplier);
                    }
                }

                if (!ReferenceEquals(source, SuppliersGrid))
                {
                    SuppliersGrid.SelectedItems.Clear();
                    foreach (var supplier in ViewModel.SelectedSuppliers.Where(ViewModel.FilteredSuppliers.Contains))
                    {
                        SuppliersGrid.SelectedItems.Add(supplier);
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
            var filteredCount = ViewModel.FilteredSuppliers.Count;
            var selectedVisibleCount = ViewModel.SelectedSuppliers.Count(ViewModel.FilteredSuppliers.Contains);

            SelectVisibleSuppliersCheckBox.IsChecked = filteredCount > 0 && selectedVisibleCount == filteredCount
                ? true
                : selectedVisibleCount == 0
                    ? false
                    : null;
        }
    }
}
