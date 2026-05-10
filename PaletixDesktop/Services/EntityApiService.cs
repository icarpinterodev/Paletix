using System.Threading;
using System.Threading.Tasks;
using SharedContracts.Common;

namespace PaletixDesktop.Services
{
    public sealed class EntityApiService<TRead, TRequest>
    {
        private readonly ApiClient _apiClient;
        private readonly string _endpoint;

        public EntityApiService(ApiClient apiClient, string endpoint)
        {
            _apiClient = apiClient;
            _endpoint = endpoint.TrimEnd('/');
        }

        public Task<PagedResult<TRead>> GetPageAsync(
            int page = 1,
            int pageSize = 25,
            CancellationToken cancellationToken = default)
        {
            return _apiClient.GetPagedAsync<TRead>(_endpoint, page, pageSize, cancellationToken);
        }

        public Task<TRead> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return _apiClient.GetAsync<TRead>($"{_endpoint}/{id}", cancellationToken);
        }

        public Task<TRead> CreateAsync(TRequest request, CancellationToken cancellationToken = default)
        {
            return _apiClient.PostAsync<TRequest, TRead>(_endpoint, request, cancellationToken);
        }

        public Task UpdateAsync(int id, TRequest request, CancellationToken cancellationToken = default)
        {
            return _apiClient.PutAsync($"{_endpoint}/{id}", request, cancellationToken);
        }

        public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            return _apiClient.DeleteAsync($"{_endpoint}/{id}", cancellationToken);
        }
    }
}
