using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PaletixDesktop.Models;
using PaletixDesktop.ViewModels;
using PaletixDesktop.Views.Shell;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PaletixDesktop.Views.Pages
{
    public sealed partial class OrdersView : UserControl, IShellCommandHandler
    {
        private bool _loaded;
        private bool _syncingSelection;

        public OrdersView()
        {
            ViewModel = new OrderCatalogViewModel();
            InitializeComponent();
            DataContext = ViewModel;
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        public OrderCatalogViewModel ViewModel { get; }

        public bool CanHandleShellCommand(string commandId) => commandId.StartsWith("orders.");

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
                case "orders.refresh":
                    await RefreshAsync();
                    break;
                case "orders.create":
                    ViewModel.OpenCreate();
                    break;
                case "orders.edit":
                    ViewModel.OpenEditSelected();
                    break;
                case "orders.delete":
                    if (await ConfirmDeleteAsync())
                    {
                        await ViewModel.DeleteSelectedAsync();
                    }
                    break;
            }

            SyncSelectionToControls();
        }

        private async void OrdersView_Loaded(object sender, RoutedEventArgs e)
        {
            if (_loaded)
            {
                return;
            }

            _loaded = true;
            await RefreshAsync();
        }

        private void OrdersSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_syncingSelection)
            {
                return;
            }

            ViewModel.SetSelection(OrdersList.SelectedItems.OfType<OrderRecord>());
            SyncSelectionToControls();
        }

        private void LineCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox { Tag: OrderRecord order })
            {
                return;
            }

            var selected = ViewModel.SelectedOrders.ToList();
            if (order.IsSelected)
            {
                if (!selected.Contains(order))
                {
                    selected.Add(order);
                }
            }
            else
            {
                selected.Remove(order);
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

            var visibleCount = ViewModel.FilteredOrders.Count;
            var selectedVisibleCount = ViewModel.SelectedOrders.Count(ViewModel.FilteredOrders.Contains);
            var allVisibleSelected = visibleCount > 0 && selectedVisibleCount == visibleCount;
            ViewModel.SetSelection(allVisibleSelected
                ? Enumerable.Empty<OrderRecord>()
                : ViewModel.FilteredOrders.ToList());
            SyncSelectionToControls();
        }

        private void OpenProductPickerButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.OpenProductPicker();
        }

        private void CloseProductPickerButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.IsProductPickerOpen = false;
        }

        private void RemoveSelectedDraftLineButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.RemoveDraftLine(DraftLinesList.SelectedItem as OrderLineRecord);
        }

        private void RemoveDraftLineButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: OrderLineRecord line })
            {
                ViewModel.RemoveDraftLine(line);
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.SaveAsync();
            SyncSelectionToControls();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ClosePanel();
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(OrderCatalogViewModel.SelectedCount) or nameof(OrderCatalogViewModel.TotalOrders))
            {
                SyncSelectionToControls();
            }
        }

        private void SyncSelectionToControls()
        {
            _syncingSelection = true;
            try
            {
                OrdersList.SelectedItems.Clear();
                foreach (var order in ViewModel.SelectedOrders.Where(ViewModel.FilteredOrders.Contains))
                {
                    OrdersList.SelectedItems.Add(order);
                }

                var filteredCount = ViewModel.FilteredOrders.Count;
                var selectedVisibleCount = ViewModel.SelectedOrders.Count(ViewModel.FilteredOrders.Contains);
                SelectVisibleOrdersCheckBox.IsChecked = filteredCount > 0 && selectedVisibleCount == filteredCount
                    ? true
                    : selectedVisibleCount == 0
                        ? false
                        : null;
            }
            finally
            {
                _syncingSelection = false;
            }
        }

        private async Task<bool> ConfirmDeleteAsync()
        {
            if (ViewModel.SelectedOrders.Count == 0)
            {
                return true;
            }

            var count = ViewModel.SelectedOrders.Count;
            var dialog = new ContentDialog
            {
                Title = count == 1 ? "Eliminar comanda" : "Eliminar comandes",
                Content = count == 1
                    ? "Vols eliminar aquesta comanda i totes les seves linies?"
                    : $"Vols eliminar {count} comandes i totes les seves linies?",
                PrimaryButtonText = "Eliminar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot
            };

            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }
    }
}
