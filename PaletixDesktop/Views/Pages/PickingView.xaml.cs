using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PaletixDesktop.Models;
using PaletixDesktop.ViewModels;
using PaletixDesktop.Views.Shell;
using System.Linq;
using System.Threading.Tasks;

namespace PaletixDesktop.Views.Pages
{
    public sealed partial class PickingView : UserControl, IShellCommandHandler
    {
        private bool _loaded;
        private bool _syncingSelection;

        public PickingView()
        {
            ViewModel = new PickingViewModel();
            InitializeComponent();
            DataContext = ViewModel;
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        public PickingViewModel ViewModel { get; }

        public bool CanHandleShellCommand(string commandId) => commandId.StartsWith("picking.");

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
                case "picking.refresh":
                    await RefreshAsync();
                    break;
                case "picking.start":
                    await ViewModel.StartPickingAsync();
                    break;
                case "picking.pause":
                    ViewModel.PausePicking();
                    break;
                case "picking.prepared":
                    await ViewModel.MarkPreparedAsync();
                    break;
                case "picking.incident":
                    await ViewModel.RegisterIncidentAsync();
                    break;
            }

            SyncSelectionToControls();
        }

        private async void PickingView_Loaded(object sender, RoutedEventArgs e)
        {
            if (_loaded)
            {
                return;
            }

            _loaded = true;
            await RefreshAsync();
        }

        private void PickingLinesSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_syncingSelection)
            {
                return;
            }

            ViewModel.SetSelection(PickingLinesList.SelectedItems.OfType<PickingLineRecord>());
            SyncSelectionToControls();
        }

        private void LineCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox { Tag: PickingLineRecord line })
            {
                return;
            }

            var selected = ViewModel.SelectedLines.ToList();
            if (line.IsSelected)
            {
                if (!selected.Contains(line))
                {
                    selected.Add(line);
                }
            }
            else
            {
                selected.Remove(line);
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

            var visibleCount = ViewModel.FilteredLines.Count;
            var selectedVisibleCount = ViewModel.SelectedLines.Count(ViewModel.FilteredLines.Contains);
            var allVisibleSelected = visibleCount > 0 && selectedVisibleCount == visibleCount;
            ViewModel.SetSelection(allVisibleSelected
                ? Enumerable.Empty<PickingLineRecord>()
                : ViewModel.FilteredLines.ToList());
            SyncSelectionToControls();
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(PickingViewModel.SelectedCount) or nameof(PickingViewModel.TotalLines))
            {
                SyncSelectionToControls();
            }
        }

        private async void MarkPreparedButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.MarkPreparedAsync();
            SyncSelectionToControls();
        }

        private async void RegisterIncidentButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.RegisterIncidentAsync();
            SyncSelectionToControls();
        }

        private async void ReleaseReservationButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.ReleaseReservationAsync();
            SyncSelectionToControls();
        }

        private void SyncSelectionToControls()
        {
            _syncingSelection = true;
            try
            {
                PickingLinesList.SelectedItems.Clear();
                foreach (var line in ViewModel.SelectedLines.Where(ViewModel.FilteredLines.Contains))
                {
                    PickingLinesList.SelectedItems.Add(line);
                }

                var filteredCount = ViewModel.FilteredLines.Count;
                var selectedVisibleCount = ViewModel.SelectedLines.Count(ViewModel.FilteredLines.Contains);
                SelectVisibleLinesCheckBox.IsChecked = filteredCount > 0 && selectedVisibleCount == filteredCount
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
    }
}
