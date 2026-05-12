using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using PaletixDesktop.ViewModels;
using System;

namespace PaletixDesktop.Views.Auth
{
    public sealed partial class LoginView : UserControl
    {
        public event EventHandler? LoginSucceeded;

        public LoginView()
        {
            ViewModel = new LoginViewModel();
            InitializeComponent();
            DataContext = ViewModel;
        }

        public LoginViewModel ViewModel { get; }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (await ViewModel.LoginAsync(PasswordInput.Password))
            {
                LoginSucceeded?.Invoke(this, EventArgs.Empty);
            }
        }

        private void PasswordInput_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                e.Handled = true;
                LoginButton_Click(sender, e);
            }
        }
    }
}
