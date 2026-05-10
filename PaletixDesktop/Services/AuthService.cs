using System.Threading;
using System.Threading.Tasks;
using PaletixDesktop.Models;

namespace PaletixDesktop.Services
{
    public sealed class AuthService
    {
        private const string CurrentUserKey = "session/current-user";
        private readonly LocalDatabase _localDatabase;

        public AuthService(LocalDatabase localDatabase)
        {
            _localDatabase = localDatabase;
        }

        public async Task<SessionUser> LoadCurrentUserAsync(CancellationToken cancellationToken = default)
        {
            await _localDatabase.InitializeAsync(cancellationToken);

            var cachedUser = await _localDatabase.GetJsonAsync<SessionUser>(CurrentUserKey, cancellationToken);
            if (cachedUser is not null)
            {
                return cachedUser;
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
    }
}
