using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PaletixDesktop.Settings;
using SharedContracts.Common;

namespace PaletixDesktop.Services
{
    public sealed class ApiClient : IDisposable
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _httpClient;
        private readonly bool _disposeClient;

        public ApiClient()
            : this(AppSettings.CreateDefault())
        {
        }

        public ApiClient(AppSettings settings, HttpClient? httpClient = null)
        {
            _httpClient = httpClient ?? new HttpClient();
            _disposeClient = httpClient is null;

            _httpClient.BaseAddress = new Uri(settings.ApiBaseUrl, UriKind.Absolute);
            _httpClient.Timeout = TimeSpan.FromSeconds(settings.RequestTimeoutSeconds);

            if (!string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                _httpClient.DefaultRequestHeaders.Remove("X-API-Key");
                _httpClient.DefaultRequestHeaders.Add("X-API-Key", settings.ApiKey);
            }
        }

        public Task<PagedResult<T>> GetPagedAsync<T>(
            string endpoint,
            int page = 1,
            int pageSize = 25,
            CancellationToken cancellationToken = default)
        {
            var separator = endpoint.Contains('?') ? '&' : '?';
            return GetAsync<PagedResult<T>>(
                $"{endpoint}{separator}page={page}&pageSize={pageSize}",
                cancellationToken);
        }

        public async Task<T> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
        {
            using var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            return await ReadResponseAsync<T>(response, cancellationToken);
        }

        public async Task<TResponse> PostAsync<TRequest, TResponse>(
            string endpoint,
            TRequest data,
            CancellationToken cancellationToken = default)
        {
            using var response = await _httpClient.PostAsJsonAsync(endpoint, data, JsonOptions, cancellationToken);
            return await ReadResponseAsync<TResponse>(response, cancellationToken);
        }

        public async Task PostAsync<TRequest>(
            string endpoint,
            TRequest data,
            CancellationToken cancellationToken = default)
        {
            using var response = await _httpClient.PostAsJsonAsync(endpoint, data, JsonOptions, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
        }

        public async Task PutAsync<TRequest>(
            string endpoint,
            TRequest data,
            CancellationToken cancellationToken = default)
        {
            var json = JsonSerializer.Serialize(data, JsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await _httpClient.PutAsync(endpoint, content, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
        }

        public async Task DeleteAsync(string endpoint, CancellationToken cancellationToken = default)
        {
            using var response = await _httpClient.DeleteAsync(endpoint, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
        }

        public void Dispose()
        {
            if (_disposeClient)
            {
                _httpClient.Dispose();
            }
        }

        private static async Task<T> ReadResponseAsync<T>(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            await EnsureSuccessAsync(response, cancellationToken);

            var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
            return value ?? throw new ApiException(response.StatusCode, "La resposta de l'API es buida.");
        }

        private static async Task EnsureSuccessAsync(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new ApiException(response.StatusCode, content);
        }
    }

    public sealed class ApiException : Exception
    {
        public ApiException(HttpStatusCode statusCode, string responseBody)
            : base($"API error {(int)statusCode}: {responseBody}")
        {
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }

        public HttpStatusCode StatusCode { get; }
        public string ResponseBody { get; }
    }
}
