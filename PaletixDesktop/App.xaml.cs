using Microsoft.UI.Xaml;
using PaletixDesktop.Services;

namespace PaletixDesktop
{
    public partial class App : Application
    {
        private Window? _window;

        public App()
        {
            Services = AppServices.CreateDefault();
            InitializeComponent();
        }

        public AppServices Services { get; }

        public static AppServices CurrentServices => ((App)Current).Services;

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            _window = new MainWindow(Services);
            _window.AppWindow.SetIcon("Assets/forklift.ico");
            _window.AppWindow.Title = "Paletix Desktop";
            _window.Activate();
        }
    }
}
