using System.Threading;
using System.Threading.Tasks;
using PaletixDesktop.Models;
using PaletixDesktop.Settings;
using SharedContracts.Dtos;

namespace PaletixDesktop.Services
{
    public sealed class AuthService
    {
        private const string CurrentUserKey = "session/current-user";
        private readonly LocalDatabase _localDatabase;
        private readonly ApiClient _apiClient;
        private readonly AppSettings _settings;

        public AuthService(LocalDatabase localDatabase, ApiClient apiClient, AppSettings settings)
        {
            _localDatabase = localDatabase;
            _apiClient = apiClient;
            _settings = settings;
        }

        public bool IsAuthenticationDisabled => _settings.DisableAuthentication;

        public async Task<SessionUser> LoadCurrentUserAsync(CancellationToken cancellationToken = default)
        {
            await _localDatabase.InitializeAsync(cancellationToken);

            var cachedUser = await _localDatabase.GetJsonAsync<SessionUser>(CurrentUserKey, cancellationToken);
            if (cachedUser is not null && (!_settings.DisableAuthentication || cachedUser.IsLocalSession))
            {
                return cachedUser;
            }

            if (!_settings.DisableAuthentication)
            {
                throw new System.InvalidOperationException("Cal iniciar sessio.");
            }

            var user = new SessionUser
            {
                Id = 1,
                Name = "Responsable",
                Surnames = "Logistica",
                RoleName = "Administrador",
                JobTitle = "Cap de magatzem",
                Level = 8,
                Points = 1840,
                IsLocalSession = true
            };

            await _localDatabase.SetJsonAsync(CurrentUserKey, user, cancellationToken);
            return user;
        }

        public async Task<bool> HasAuthenticatedSessionAsync(CancellationToken cancellationToken = default)
        {
            if (_settings.DisableAuthentication)
            {
                return true;
            }

            await _localDatabase.InitializeAsync(cancellationToken);
            var cachedUser = await _localDatabase.GetJsonAsync<SessionUser>(CurrentUserKey, cancellationToken);
            return cachedUser is not null && !cachedUser.IsLocalSession;
        }

        public async Task<SessionUser> LoginAsync(string identifier, string password, CancellationToken cancellationToken = default)
        {
            var response = await _apiClient.PostAsync<LoginRequestDto, LoginResponseDto>(
                "api/Auth/login",
                new LoginRequestDto
                {
                    Identifier = identifier,
                    Password = password
                },
                cancellationToken);

            var user = new SessionUser
            {
                Id = response.Id,
                Name = response.Nom,
                Surnames = response.Cognoms,
                RoleName = response.Rol,
                JobTitle = response.Carrec,
                Level = response.Nivell,
                Points = response.SaldoPunts,
                Permissions = response.Permissions,
                IsLocalSession = false
            };

            await _localDatabase.InitializeAsync(cancellationToken);
            await _localDatabase.SetJsonAsync(CurrentUserKey, user, cancellationToken);
            return user;
        }

        public async Task LogoutAsync(CancellationToken cancellationToken = default)
        {
            await _localDatabase.InitializeAsync(cancellationToken);
            await _localDatabase.RemoveAsync(CurrentUserKey, cancellationToken);
        }
    }
}
