using System.Collections.ObjectModel;
using System.Linq;
using PaletixDesktop.Models;
using PaletixDesktop.Services;

namespace PaletixDesktop.ViewModels
{
    public sealed class ModulePageViewModel : ViewModelBase
    {
        public ModulePageViewModel(ModuleDefinition module, PermissionService permissionService)
        {
            Module = module;
            CanCreate = permissionService.CanAccess(module.Feature, PermissionAction.Create);
            CanEdit = permissionService.CanAccess(module.Feature, PermissionAction.Edit);
            CanSync = permissionService.CanAccess(module.Feature, PermissionAction.Sync);

            foreach (var metric in module.Metrics)
            {
                Metrics.Add(metric);
            }

            foreach (var item in module.WorkItems)
            {
                WorkItems.Add(item);
            }

            foreach (var action in module.PrimaryActions.Take(3))
            {
                PrimaryActions.Add(action);
            }
        }

        public ModuleDefinition Module { get; }
        public string Title => Module.Title;
        public string Subtitle => Module.Subtitle;
        public bool CanCreate { get; }
        public bool CanEdit { get; }
        public bool CanSync { get; }
        public ObservableCollection<DashboardMetric> Metrics { get; } = new();
        public ObservableCollection<ActivityItem> WorkItems { get; } = new();
        public ObservableCollection<string> PrimaryActions { get; } = new();
    }
}
