using System;

namespace PaletixDesktop.Settings
{
    public sealed class AppSettings
    {
        public string ApiBaseUrl { get; init; } = AppConstants.ApiBaseUrl;
        public string ApiKey { get; init; } = AppConstants.ApiKey;
        public int RequestTimeoutSeconds { get; init; } = 30;
        public bool DisableAuthentication { get; init; } = AppConstants.DisableAuthentication;

        public static AppSettings CreateDefault()
        {
            return new AppSettings
            {
                ApiBaseUrl = Environment.GetEnvironmentVariable("PALETIX_API_BASE_URL") ?? AppConstants.ApiBaseUrl,
                ApiKey = Environment.GetEnvironmentVariable("PALETIX_API_KEY") ?? AppConstants.ApiKey,
                DisableAuthentication = ReadBoolean("PALETIX_DISABLE_AUTH", AppConstants.DisableAuthentication)
            };
        }

        private static bool ReadBoolean(string name, bool fallback)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            return value.Equals("1", StringComparison.OrdinalIgnoreCase)
                || value.Equals("true", StringComparison.OrdinalIgnoreCase)
                || value.Equals("yes", StringComparison.OrdinalIgnoreCase)
                || value.Equals("si", StringComparison.OrdinalIgnoreCase);
        }
    }
}
