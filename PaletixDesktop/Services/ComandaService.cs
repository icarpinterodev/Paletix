using System.Threading;
using System.Threading.Tasks;
using SharedContracts.Common;
using SharedContracts.Dtos;

namespace PaletixDesktop.Services
{
    public sealed class ComandaService
    {
        private readonly ApiClient _api;

        public ComandaService(ApiClient api)
        {
            _api = api;
        }

        public Task<PagedResult<ProductesReadDto>> GetProductesAsync(
            int page = 1,
            int pageSize = 25,
            CancellationToken cancellationToken = default)
        {
            return _api.GetPagedAsync<ProductesReadDto>("api/productes", page, pageSize, cancellationToken);
        }

        public Task<PagedResult<ComandesReadDto>> GetComandesAsync(
            int page = 1,
            int pageSize = 25,
            CancellationToken cancellationToken = default)
        {
            return _api.GetPagedAsync<ComandesReadDto>("api/comandes", page, pageSize, cancellationToken);
        }
    }
}
