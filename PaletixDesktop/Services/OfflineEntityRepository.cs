using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PaletixDesktop.Services
{
    public sealed class OfflineEntityRepository<TRead, TRequest>
    {
        private readonly string _entityName;
        private readonly string _endpoint;
        private readonly EntityApiService<TRead, TRequest> _api;
        private readonly LocalDatabase _localDatabase;
        private readonly MemoryEntityCache _memoryCache;
        private readonly SyncQueue _syncQueue;

        public OfflineEntityRepository(
            string entityName,
            string endpoint,
            EntityApiService<TRead, TRequest> api,
            LocalDatabase localDatabase,
            MemoryEntityCache memoryCache,
            SyncQueue syncQueue)
        {
            _entityName = entityName;
            _endpoint = endpoint.TrimEnd('/');
            _api = api;
            _localDatabase = localDatabase;
            _memoryCache = memoryCache;
            _syncQueue = syncQueue;
        }

        public async Task<IReadOnlyList<TRead>> GetPageAsync(
            int page = 1,
            int pageSize = 25,
            CancellationToken cancellationToken = default)
        {
            var key = $"{_entityName}/page/{page}/{pageSize}";
            if (_memoryCache.TryGet<IReadOnlyList<TRead>>(key, out var cached) && cached is not null)
            {
                return cached;
            }

            try
            {
                var result = await _api.GetPageAsync(page, pageSize, cancellationToken);
                var items = new List<TRead>(result.Items);
                _memoryCache.Set<IReadOnlyList<TRead>>(key, items);
                await _localDatabase.SetJsonAsync(key, items, cancellationToken);
                return items;
            }
            catch (Exception ex) when (IsConnectivityFailure(ex))
            {
                var localItems = await _localDatabase.GetJsonAsync<List<TRead>>(key, cancellationToken);
                return localItems is null ? Array.Empty<TRead>() : localItems;
            }
        }

        public async Task<TRead?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var key = $"{_entityName}/item/{id}";
            if (_memoryCache.TryGet<TRead>(key, out var cached))
            {
                return cached;
            }

            try
            {
                var item = await _api.GetByIdAsync(id, cancellationToken);
                _memoryCache.Set(key, item);
                await _localDatabase.SetJsonAsync(key, item, cancellationToken);
                return item;
            }
            catch (Exception ex) when (IsConnectivityFailure(ex))
            {
                return await _localDatabase.GetJsonAsync<TRead>(key, cancellationToken);
            }
        }

        public async Task<TRead?> CreateAsync(TRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _api.CreateAsync(request, cancellationToken);
            }
            catch (Exception ex) when (IsConnectivityFailure(ex))
            {
                await _syncQueue.EnqueueAsync(_entityName, null, "POST", _endpoint, request!, cancellationToken);
                return default;
            }
        }

        public async Task UpdateAsync(int id, TRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                await _api.UpdateAsync(id, request, cancellationToken);
            }
            catch (Exception ex) when (IsConnectivityFailure(ex))
            {
                await _syncQueue.EnqueueAsync(_entityName, id.ToString(), "PUT", $"{_endpoint}/{id}", request!, cancellationToken);
            }
        }

        public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                await _api.DeleteAsync(id, cancellationToken);
            }
            catch (Exception ex) when (IsConnectivityFailure(ex))
            {
                await _syncQueue.EnqueueAsync(_entityName, id.ToString(), "DELETE", $"{_endpoint}/{id}", new { id }, cancellationToken);
            }
        }

        private static bool IsConnectivityFailure(Exception ex)
        {
            return ex is HttpRequestException or TaskCanceledException;
        }
    }
}
