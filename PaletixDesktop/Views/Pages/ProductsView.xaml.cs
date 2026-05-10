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
    public sealed partial class ProductsView : UserControl, IShellCommandHandler
    {
        private bool _syncingSelection;
        private bool _loaded;

        public ProductsView()
        {
            ViewModel = new ProductCatalogViewModel();
            InitializeComponent();
            DataContext = ViewModel;
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        public ProductCatalogViewModel ViewModel { get; }

        public bool CanHandleShellCommand(string commandId)
        {
            return commandId.StartsWith("products.");
        }

        public void HandleShellCommand(string commandId)
        {
            _ = HandleShellCommandAsync(commandId);
        }

        private async Task HandleShellCommandAsync(string commandId)
        {
            switch (commandId)
            {
                case "products.refresh":
                    await RefreshAsync();
                    break;
                case "products.create":
                    ViewModel.CreateProduct();
                    break;
                case "products.edit":
                    ViewModel.StartEditSelected();
                    break;
                case "products.delete":
                    await DeleteSelectedWithConfirmationAsync();
                    break;
                case "products.import":
                    await ViewModel.ImportProductsAsync();
                    break;
                case "products.view.table":
                    ViewModel.SetViewMode(ProductCatalogViewMode.Table);
                    break;
                case "products.view.grid":
                    ViewModel.SetViewMode(ProductCatalogViewMode.Grid);
                    break;
            }

            SyncSelectionToControls();
        }

        private async void ProductsView_Loaded(object sender, RoutedEventArgs e)
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

        private void ProductsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_syncingSelection)
            {
                return;
            }

            var source = sender as ListViewBase;
            ViewModel.SetSelection(source?.SelectedItems.OfType<ProductRecord>() ?? Enumerable.Empty<ProductRecord>());
            SyncSelectionToControls(source);
        }

        private void EditProductButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { Tag: ProductRecord product })
            {
                ViewModel.SelectSingle(product);
                ViewModel.StartEditSelected();
                SyncSelectionToControls();
            }
        }

        private async void DeleteProductButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { Tag: ProductRecord product })
            {
                ViewModel.SelectSingle(product);
                await DeleteSelectedWithConfirmationAsync();
                SyncSelectionToControls();
            }
        }

        private async void SaveCreateButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.SavePanelAsync();
            SyncSelectionToControls();
        }

        private void CancelCreateButton_Click(object sender, RoutedEventArgs e)
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

        private void ProductCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox { Tag: ProductRecord product })
            {
                return;
            }

            var selected = ViewModel.SelectedProducts.ToList();
            if (product.IsSelected)
            {
                if (!selected.Contains(product))
                {
                    selected.Add(product);
                }
            }
            else
            {
                selected.Remove(product);
            }

            ViewModel.SetSelection(selected);
            SyncSelectionToControls();
        }

        private void ProductsHeaderCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (_syncingSelection)
            {
                return;
            }

            var visibleCount = ViewModel.FilteredProducts.Count;
            var selectedVisibleCount = ViewModel.SelectedProducts.Count(ViewModel.FilteredProducts.Contains);
            var allVisibleSelected = visibleCount > 0 && selectedVisibleCount == visibleCount;

            ViewModel.SetSelection(allVisibleSelected
                ? Enumerable.Empty<ProductRecord>()
                : ViewModel.FilteredProducts.ToList());

            SyncSelectionToControls();
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(ProductCatalogViewModel.FilteredCount) or nameof(ProductCatalogViewModel.SelectedCount))
            {
                SyncSelectionToControls();
            }
        }

        private void ProductsTable_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
        {
            var menu = new MenuFlyout();

            if (ViewModel.SelectedCount == 0)
            {
                menu.Items.Add(MenuItem("Nou producte", "\uE710", () => ViewModel.CreateProduct()));
                menu.Items.Add(MenuItem("Importar productes", "\uE8B5", () => _ = ImportProductsFromMenuAsync()));
            }
            else if (ViewModel.SelectedCount == 1)
            {
                menu.Items.Add(MenuItem("Editar producte", "\uE70F", () => ViewModel.StartEditSelected()));
                menu.Items.Add(MenuItem("Eliminar producte", "\uE74D", () =>
                {
                    _ = DeleteSelectedFromMenuAsync();
                }));
                menu.Items.Add(MenuItem("Obrir detall", "\uE8A7", () => ViewModel.StartEditSelected()));
            }
            else
            {
                menu.Items.Add(MenuItem("Editar seleccio", "\uE70F", () => ViewModel.StartEditSelected()));
                menu.Items.Add(MenuItem("Eliminar seleccio", "\uE74D", () =>
                {
                    _ = DeleteSelectedFromMenuAsync();
                }));
                menu.Items.Add(MenuItem("Exportar seleccio", "\uE896", () => { }));
            }

            menu.ShowAt(sender as FrameworkElement);
            args.Handled = true;
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

            if (!await ConfirmDeleteAsync("producte", "productes"))
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

        private async Task ImportProductsFromMenuAsync()
        {
            await ViewModel.ImportProductsAsync();
            SyncSelectionToControls();
        }

        private void SyncSelectionToControls(ListViewBase? source = null)
        {
            _syncingSelection = true;
            try
            {
                if (!ReferenceEquals(source, ProductsTable))
                {
                    ProductsTable.SelectedItems.Clear();
                    foreach (var product in ViewModel.SelectedProducts.Where(ViewModel.FilteredProducts.Contains))
                    {
                        ProductsTable.SelectedItems.Add(product);
                    }
                }

                if (!ReferenceEquals(source, ProductsGrid))
                {
                    ProductsGrid.SelectedItems.Clear();
                    foreach (var product in ViewModel.SelectedProducts.Where(ViewModel.FilteredProducts.Contains))
                    {
                        ProductsGrid.SelectedItems.Add(product);
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
            var filteredCount = ViewModel.FilteredProducts.Count;
            var selectedVisibleCount = ViewModel.SelectedProducts.Count(ViewModel.FilteredProducts.Contains);

            SelectVisibleProductsCheckBox.IsChecked = filteredCount > 0 && selectedVisibleCount == filteredCount
                ? true
                : selectedVisibleCount == 0
                    ? false
                    : null;
        }
    }
}
