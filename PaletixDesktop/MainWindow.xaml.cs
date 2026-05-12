using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using PaletixDesktop.Services;
using PaletixDesktop.Settings;
using PaletixDesktop.Views.Auth;
using PaletixDesktop.Views.Shell;
using Windows.Graphics;

namespace PaletixDesktop
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
            : this(App.CurrentServices)
        {
        }

        public MainWindow(AppServices services)
        {
            Services = services;
            InitializeComponent();
            InitializeRootContent();
            ApplyWindowOptions();
            SizeChanged += MainWindow_SizeChanged;
        }

        public AppServices Services { get; }

        private void InitializeRootContent()
        {
            if (Services.Settings.DisableAuthentication)
            {
                ShowShell();
                return;
            }

            ShowLogin();
        }

        private void ShowLogin()
        {
            var login = new LoginView();
            login.LoginSucceeded += (_, _) => ShowShell();
            RootContent.Content = login;
        }

        private void ShowShell()
        {
            var shell = new ShellView();
            shell.LogoutRequested += async (_, _) =>
            {
                await Services.AuthService.LogoutAsync();
                if (Services.Settings.DisableAuthentication)
                {
                    ShowShell();
                }
                else
                {
                    ShowLogin();
                }
            };
            RootContent.Content = shell;
        }

        private void ApplyWindowOptions()
        {
            AppWindow.Resize(new SizeInt32(AppConstants.InitialWindowWidth, AppConstants.InitialWindowHeight));
            ConfigureTitleBar();
        }

        private void ConfigureTitleBar()
        {
            var titleBar = AppWindow.TitleBar;
            if (AppConstants.UseTransparentTitleBar)
            {
                titleBar.BackgroundColor = Colors.Transparent;
                titleBar.InactiveBackgroundColor = Colors.Transparent;
                titleBar.ButtonBackgroundColor = Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                titleBar.ButtonForegroundColor = Colors.White;
                titleBar.ButtonInactiveForegroundColor = Colors.Gray;
                titleBar.ButtonHoverBackgroundColor = ColorHelper.FromArgb(80, 255, 255, 255);
                titleBar.ButtonPressedBackgroundColor = ColorHelper.FromArgb(120, 255, 255, 255);
                return;
            }

            titleBar.BackgroundColor = ColorHelper.FromArgb(255, 21, 27, 36);
            titleBar.InactiveBackgroundColor = ColorHelper.FromArgb(255, 21, 27, 36);
            titleBar.ButtonBackgroundColor = ColorHelper.FromArgb(255, 21, 27, 36);
            titleBar.ButtonInactiveBackgroundColor = ColorHelper.FromArgb(255, 21, 27, 36);
            titleBar.ButtonForegroundColor = Colors.White;
            titleBar.ButtonInactiveForegroundColor = Colors.Gray;
        }

        private void MainWindow_SizeChanged(object sender, WindowSizeChangedEventArgs args)
        {
            var current = AppWindow.Size;
            var width = Clamp(current.Width, AppConstants.MinWindowWidth, AppConstants.MaxWindowWidth);
            var height = Clamp(current.Height, AppConstants.MinWindowHeight, AppConstants.MaxWindowHeight);

            if (width != current.Width || height != current.Height)
            {
                AppWindow.Resize(new SizeInt32(width, height));
            }
        }

        private static int Clamp(int value, int min, int max)
        {
            return value < min ? min : value > max ? max : value;
        }
    }
}
