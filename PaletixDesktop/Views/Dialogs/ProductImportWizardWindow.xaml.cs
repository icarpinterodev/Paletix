using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using PaletixDesktop.ViewModels;
using System;
using System.Runtime.InteropServices;
using Windows.Graphics;
using WinRT.Interop;

namespace PaletixDesktop.Views.Dialogs
{
    public sealed partial class ProductImportWizardWindow : Window
    {
        private const int WindowWidth = 1400;
        private const int WindowHeight = 860;
        private readonly ProductCatalogViewModel _viewModel;
        private readonly IntPtr _ownerHwnd;
        private bool _ownerDisabled;
        private bool _finishedOrBackground;

        public ProductImportWizardWindow(ProductCatalogViewModel viewModel)
        {
            _viewModel = viewModel;
            InitializeComponent();
            AppWindow.Title = "Assistent d'importacio de productes";
            AppWindow.SetIcon("Assets/forklift.ico");
            ConfigurePresenter();

            var content = new ProductImportWizardDialog(viewModel);
            content.CancelRequested += (_, _) => Close();
            content.ImportCompleted += (_, _) =>
            {
                _finishedOrBackground = true;
                Close();
            };
            content.ImportStartedInBackground += (_, _) =>
            {
                _finishedOrBackground = true;
                Close();
            };
            Host.Children.Add(content);

            _ownerHwnd = App.MainWindowInstance is null
                ? IntPtr.Zero
                : WindowNative.GetWindowHandle(App.MainWindowInstance);

            Closed += ProductImportWizardWindow_Closed;
            Activated += ProductImportWizardWindow_Activated;
        }

        public void ShowModal()
        {
            DisableOwner();
            CenterOverOwner();
            Activate();
        }

        private void ConfigurePresenter()
        {
            AppWindow.Resize(new SizeInt32(WindowWidth, WindowHeight));
            if (AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsMaximizable = false;
                presenter.IsMinimizable = false;
                presenter.IsResizable = true;
            }
        }

        private void CenterOverOwner()
        {
            if (App.MainWindowInstance is null)
            {
                return;
            }

            var ownerWindow = App.MainWindowInstance.AppWindow;
            var ownerSize = ownerWindow.Size;
            var ownerPosition = ownerWindow.Position;
            var x = ownerPosition.X + Math.Max(0, (ownerSize.Width - WindowWidth) / 2);
            var y = ownerPosition.Y + Math.Max(0, (ownerSize.Height - WindowHeight) / 2);
            AppWindow.MoveAndResize(new RectInt32(x, y, WindowWidth, WindowHeight));
        }

        private void DisableOwner()
        {
            if (_ownerHwnd == IntPtr.Zero || _ownerDisabled)
            {
                return;
            }

            EnableWindow(_ownerHwnd, false);
            _ownerDisabled = true;
        }

        private void EnableOwner()
        {
            if (_ownerHwnd == IntPtr.Zero || !_ownerDisabled)
            {
                return;
            }

            EnableWindow(_ownerHwnd, true);
            _ownerDisabled = false;
            App.MainWindowInstance?.Activate();
        }

        private void ProductImportWizardWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (_ownerDisabled && args.WindowActivationState == WindowActivationState.Deactivated)
            {
                DispatcherQueue.TryEnqueue(Activate);
            }
        }

        private void ProductImportWizardWindow_Closed(object sender, WindowEventArgs args)
        {
            if (!_finishedOrBackground && !_viewModel.IsImporting)
            {
                _viewModel.CancelImportPanel();
            }

            EnableOwner();
        }

        [DllImport("user32.dll")]
        private static extern bool EnableWindow(IntPtr hWnd, bool bEnable);
    }
}
