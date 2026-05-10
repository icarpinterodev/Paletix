using System.Collections.ObjectModel;
using PaletixDesktop.Models;
using PaletixDesktop.Services;

namespace PaletixDesktop.ViewModels
{
    public sealed class DashboardViewModel : ViewModelBase
    {
        private readonly PermissionService _permissionService;

        public DashboardViewModel(ShellViewModel shell, PermissionService permissionService)
        {
            _permissionService = permissionService;
            UserName = shell.UserDisplayName;
            RoleLine = shell.UserRoleLine;
            ProgressLine = shell.UserProgressLine;

            Load();
        }

        public string UserName { get; }
        public string RoleLine { get; }
        public string ProgressLine { get; }

        public ObservableCollection<DashboardMetric> Metrics { get; } = new();
        public ObservableCollection<QuickAction> QuickActions { get; } = new();
        public ObservableCollection<ActivityItem> Activity { get; } = new();

        private void Load()
        {
            Metrics.Add(new DashboardMetric { Title = "Comandes obertes", Value = "42", Detail = "11 en preparacio", State = "Actiu" });
            Metrics.Add(new DashboardMetric { Title = "Stock critic", Value = "12", Detail = "4 afecten comandes", State = "Atencio" });
            Metrics.Add(new DashboardMetric { Title = "Productivitat", Value = "91%", Detail = "Equip per sobre objectiu", State = "Correcte" });
            Metrics.Add(new DashboardMetric { Title = "Sync local", Value = "0", Detail = "Canvis pendents", State = "Correcte" });

            AddAction("Preparar comandes", "Operacions del dia", "operations", AppFeature.Operations);
            AddAction("Revisar stock", "Alertes i ubicacions", "warehouse", AppFeature.Warehouse);
            AddAction("Gamificacio", "Reptes i premis", "gamification", AppFeature.Gamification);
            AddAction("Administracio", "API, permisos i sync", "admin", AppFeature.Administration);

            Activity.Add(new ActivityItem { Title = "Comanda COM-1045", Detail = "Assignada a ruta B-12", Time = "Fa 12 min", State = "En curs" });
            Activity.Add(new ActivityItem { Title = "Stock SKU-1440", Detail = "Reposicio recomanada a Z2P4", Time = "Fa 24 min", State = "Atencio" });
            Activity.Add(new ActivityItem { Title = "Repte setmanal", Detail = "Equip de mati al 74%", Time = "Avui", State = "Actiu" });
            Activity.Add(new ActivityItem { Title = "SQLite local", Detail = "Base preparada per treball offline", Time = "Sistema", State = "Correcte" });
        }

        private void AddAction(string title, string detail, string route, AppFeature feature)
        {
            if (!_permissionService.CanAccess(feature))
            {
                return;
            }

            QuickActions.Add(new QuickAction
            {
                Title = title,
                Detail = detail,
                Route = route,
                Feature = feature
            });
        }
    }
}
