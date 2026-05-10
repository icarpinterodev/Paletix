using PaletixDesktop.Settings;
using PaletixDesktop.ViewModels;

namespace PaletixDesktop.Services
{
    public sealed class AppServices
    {
        private AppServices(AppSettings settings)
        {
            Settings = settings;
            ApiClient = new ApiClient(settings);
            LocalDatabase = new LocalDatabase();
            MemoryCache = new MemoryEntityCache();
            SyncQueue = new SyncQueue(LocalDatabase);
            NotificationService = new NotificationService();
            LookupDataService = new LookupDataService(ApiClient, LocalDatabase);
            ClientDataService = new ClientDataService(ApiClient, LocalDatabase, SyncQueue);
            SupplierDataService = new SupplierDataService(ApiClient, LocalDatabase, SyncQueue);
            ProductDataService = new ProductDataService(ApiClient, LocalDatabase, SyncQueue);
            StockDataService = new StockDataService(ApiClient, LocalDatabase, SyncQueue);
            LocationDataService = new LocationDataService(ApiClient, LocalDatabase, SyncQueue);
            SupplierLotDataService = new SupplierLotDataService(ApiClient, LocalDatabase, SyncQueue);
            PendingSyncService = new PendingSyncService(
                SyncQueue,
                LocalDatabase,
                ClientDataService,
                SupplierDataService,
                ProductDataService,
                StockDataService,
                LocationDataService,
                SupplierLotDataService);
            AuthService = new AuthService(LocalDatabase);
            PermissionService = new PermissionService();
            NavigationService = new NavigationService();
            ShellNavigationCatalog = new ShellNavigationCatalog();
            ModuleCatalog = new ModuleCatalog();
            ComandaService = new ComandaService(ApiClient);
            ShellViewModel = new ShellViewModel(AuthService, PermissionService, SyncQueue);
        }

        public AppSettings Settings { get; }
        public ApiClient ApiClient { get; }
        public LocalDatabase LocalDatabase { get; }
        public MemoryEntityCache MemoryCache { get; }
        public SyncQueue SyncQueue { get; }
        public NotificationService NotificationService { get; }
        public LookupDataService LookupDataService { get; }
        public ClientDataService ClientDataService { get; }
        public SupplierDataService SupplierDataService { get; }
        public ProductDataService ProductDataService { get; }
        public StockDataService StockDataService { get; }
        public LocationDataService LocationDataService { get; }
        public SupplierLotDataService SupplierLotDataService { get; }
        public PendingSyncService PendingSyncService { get; }
        public AuthService AuthService { get; }
        public PermissionService PermissionService { get; }
        public NavigationService NavigationService { get; }
        public ShellNavigationCatalog ShellNavigationCatalog { get; }
        public ModuleCatalog ModuleCatalog { get; }
        public ComandaService ComandaService { get; }
        public ShellViewModel ShellViewModel { get; }

        public static AppServices CreateDefault()
        {
            return new AppServices(AppSettings.CreateDefault());
        }

        public DashboardViewModel CreateDashboardViewModel()
        {
            return new DashboardViewModel(ShellViewModel, PermissionService);
        }

        public ModulePageViewModel CreateModuleViewModel(string route)
        {
            return new ModulePageViewModel(ModuleCatalog.GetModule(route), PermissionService);
        }

        public OfflineEntityRepository<TRead, TRequest> CreateRepository<TRead, TRequest>(
            string entityName,
            string endpoint)
        {
            var apiService = new EntityApiService<TRead, TRequest>(ApiClient, endpoint);
            return new OfflineEntityRepository<TRead, TRequest>(
                entityName,
                endpoint,
                apiService,
                LocalDatabase,
                MemoryCache,
                SyncQueue);
        }
    }
}
