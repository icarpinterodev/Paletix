using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PaletixDesktop.ViewModels;
using System;
using System.ComponentModel;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace PaletixDesktop.Views.Dialogs
{
    public sealed partial class ProductImportWizardDialog : UserControl
    {
        private bool _updatingStep;

        public ProductImportWizardDialog(ProductCatalogViewModel viewModel)
        {
            ViewModel = viewModel;
            InitializeComponent();
            DataContext = ViewModel;
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            Unloaded += ProductImportWizardDialog_Unloaded;
            UpdateNavigationButtons();
        }

        public ProductCatalogViewModel ViewModel { get; }
        public event EventHandler? CancelRequested;
        public event EventHandler? ImportCompleted;
        public event EventHandler? ImportStartedInBackground;
        public event EventHandler? StepChanged;
        public int CurrentStep => WizardPivot.SelectedIndex;
        public bool CanGoBack => CurrentStep > 0;
        public bool IsFinalStep => CurrentStep == 2;

        private async void LoadImportFileButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            picker.FileTypeFilter.Add(".csv");
            picker.FileTypeFilter.Add(".json");

            if (App.MainWindowInstance is not null)
            {
                InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindowInstance));
            }

            var file = await picker.PickSingleFileAsync();
            if (file is null)
            {
                return;
            }

            ViewModel.ImportFormat = string.Equals(file.FileType, ".json", StringComparison.OrdinalIgnoreCase)
                ? "json"
                : "csv";
            ViewModel.ImportRawText = await FileIO.ReadTextAsync(file);
            ViewModel.ParseImportPreview();
            WizardPivot.SelectedIndex = 1;
        }

        private void PreviewImportButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ParseImportPreview();
        }

        public async System.Threading.Tasks.Task<bool> ApplyImportAsync()
        {
            return await ViewModel.ApplyImportAsync();
        }

        private async void ApplyImportButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ViewModel.CanApplyImport)
            {
                ViewModel.ParseImportPreview();
                UpdateNavigationButtons();
                return;
            }

            if (ViewModel.ImportRunsInBackground)
            {
                _ = ApplyImportInBackgroundAsync();
                ImportStartedInBackground?.Invoke(this, EventArgs.Empty);
                return;
            }

            if (await ApplyImportAsync())
            {
                ImportCompleted?.Invoke(this, EventArgs.Empty);
            }
        }

        private async System.Threading.Tasks.Task ApplyImportInBackgroundAsync()
        {
            await ApplyImportAsync();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.CancelImportPanel();
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            GoBack();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            GoNext();
        }

        public void GoBack()
        {
            if (WizardPivot.SelectedIndex > 0)
            {
                WizardPivot.SelectedIndex--;
            }
        }

        public void GoNext()
        {
            if (WizardPivot.SelectedIndex == 0)
            {
                DetectFormatFromTextIfNeeded();
                ViewModel.ParseImportPreview();
                if (ViewModel.ImportPreviewRows.Count == 0)
                {
                    UpdateNavigationButtons();
                    return;
                }
            }

            if (WizardPivot.SelectedIndex == 1 && ViewModel.ValidImportCount == 0)
            {
                ViewModel.ParseImportPreview();
                UpdateNavigationButtons();
                return;
            }

            if (WizardPivot.SelectedIndex < 2)
            {
                WizardPivot.SelectedIndex++;
            }
        }

        private void WizardPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_updatingStep)
            {
                return;
            }

            if (WizardPivot.SelectedIndex == 1 && ViewModel.ImportPreviewRows.Count == 0 && !string.IsNullOrWhiteSpace(ViewModel.ImportRawText))
            {
                DetectFormatFromTextIfNeeded();
                ViewModel.ParseImportPreview();
            }

            if (WizardPivot.SelectedIndex == 1 && !ViewModel.HasImportRawText)
            {
                ViewModel.ParseImportPreview();
                MoveToStep(0);
                return;
            }

            if (WizardPivot.SelectedIndex == 2 && !ViewModel.CanGoToImportStep)
            {
                ViewModel.ParseImportPreview();
                MoveToStep(ViewModel.HasImportRawText ? 1 : 0);
                return;
            }

            UpdateNavigationButtons();
            StepChanged?.Invoke(this, EventArgs.Empty);
        }

        private void MoveToStep(int index)
        {
            if (WizardPivot.SelectedIndex == index)
            {
                UpdateNavigationButtons();
                StepChanged?.Invoke(this, EventArgs.Empty);
                return;
            }

            _updatingStep = true;
            if (!DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    WizardPivot.SelectedIndex = index;
                }
                finally
                {
                    _updatingStep = false;
                }

                UpdateNavigationButtons();
                StepChanged?.Invoke(this, EventArgs.Empty);
            }))
            {
                _updatingStep = false;
                UpdateNavigationButtons();
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(ProductCatalogViewModel.ImportRawText)
                or nameof(ProductCatalogViewModel.ValidImportCount)
                or nameof(ProductCatalogViewModel.CanApplyImport)
                or nameof(ProductCatalogViewModel.IsImporting))
            {
                UpdateNavigationButtons();
            }
        }

        private void ProductImportWizardDialog_Unloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }

        private void UpdateNavigationButtons()
        {
            if (BackButton is null || NextButton is null || ImportButton is null)
            {
                return;
            }

            BackButton.IsEnabled = CanGoBack && !ViewModel.IsImporting;
            NextButton.Visibility = IsFinalStep ? Visibility.Collapsed : Visibility.Visible;
            NextButton.IsEnabled = CurrentStep switch
            {
                0 => ViewModel.CanPreviewImport,
                1 => ViewModel.CanGoToImportStep,
                _ => false
            };
            ImportButton.Visibility = IsFinalStep ? Visibility.Visible : Visibility.Collapsed;
        }

        private void DetectFormatFromTextIfNeeded()
        {
            var text = ViewModel.ImportRawText.TrimStart();
            if (text.StartsWith("[", StringComparison.Ordinal) || text.StartsWith("{", StringComparison.Ordinal))
            {
                ViewModel.ImportFormat = "json";
            }
        }
    }
}
