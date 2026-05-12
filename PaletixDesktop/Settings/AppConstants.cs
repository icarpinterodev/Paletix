namespace PaletixDesktop.Settings
{
    public static class AppConstants
    {
        public const string AppName = "Paletix";
        public const string ApiBaseUrl = "https://localhost:7137/";
        public const string ApiKey = "clau-api-complexa-564";
        public const string LocalDatabaseName = "paletix-local.db";
        public const bool DisableAuthentication = false;

        public const int InitialWindowWidth = 1500;
        public const int InitialWindowHeight = 900;
        public const int MinWindowWidth = 1180;
        public const int MinWindowHeight = 720;
        public const int MaxWindowWidth = 2560;
        public const int MaxWindowHeight = 1440;
        public const int ConnectionCheckIntervalSeconds = 8;
        public const int ConnectionCheckTimeoutSeconds = 3;
        public static bool UseTransparentTitleBar => false;
        public static bool UseMicaBackdrop => true;
        public static bool CenterInitialWindow => true;
    }
}
