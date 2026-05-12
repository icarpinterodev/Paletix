using PaletixDesktop.Services;
using System;
using System.Threading.Tasks;

namespace PaletixDesktop.ViewModels
{
    public sealed class LoginViewModel : ViewModelBase
    {
        private readonly AuthService _authService;
        private string _identifier = "";
        private string _statusText = "Inicia sessio amb email o DNI.";

        public LoginViewModel()
            : this(App.CurrentServices.AuthService)
        {
        }

        public LoginViewModel(AuthService authService)
        {
            _authService = authService;
        }

        public string Identifier
        {
            get => _identifier;
            set => SetProperty(ref _identifier, value);
        }

        public string StatusText
        {
            get => _statusText;
            private set => SetProperty(ref _statusText, value);
        }

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        public async Task<bool> LoginAsync(string password)
        {
            if (string.IsNullOrWhiteSpace(Identifier) || string.IsNullOrWhiteSpace(password))
            {
                StatusText = "Introdueix email/DNI i contrasenya.";
                return false;
            }

            if (password.Length < 8)
            {
                StatusText = "La contrasenya ha de tenir com a minim 8 caracters.";
                return false;
            }

            IsBusy = true;
            ErrorMessage = null;
            OnPropertyChanged(nameof(HasError));
            try
            {
                await _authService.LoginAsync(Identifier.Trim(), password);
                StatusText = "Sessio iniciada.";
                return true;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                OnPropertyChanged(nameof(HasError));
                StatusText = "No s'ha pogut iniciar sessio. Revisa les credencials o la connexio.";
                return false;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
