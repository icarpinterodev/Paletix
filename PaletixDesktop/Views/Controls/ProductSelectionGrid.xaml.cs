using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PaletixDesktop.Models;
using PaletixDesktop.ViewModels;

namespace PaletixDesktop.Views.Controls
{
    public sealed partial class ProductSelectionGrid : UserControl
    {
        public ProductSelectionGrid()
        {
            InitializeComponent();
        }

        private void ProductCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (DataContext is OrderCatalogViewModel viewModel && sender is CheckBox { Tag: OrderProductPickerItem item } checkBox)
            {
                viewModel.ToggleProductFromPicker(item, checkBox.IsChecked == true);
            }
        }
    }
}
