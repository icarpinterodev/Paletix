using System;

namespace PaletixDesktop.Settings
{
    public sealed class AppSettings
    {
        public string ApiBaseUrl { get; init; } = AppConstants.ApiBaseUrl;
        public string ApiKey { get; init; } = AppConstants.ApiKey;
        public int RequestTimeoutSeconds { get; init; } = 30;

        public static AppSettings CreateDefault()
        {
            return new AppSettings
            {
                ApiBaseUrl = Environment.GetEnvironmentVariable("https://localhost:7137/") ?? AppConstants.ApiBaseUrl,
                ApiKey = Environment.GetEnvironmentVariable("clau-api-complexa-564") ?? AppConstants.ApiKey
            };
        }
    }
}
