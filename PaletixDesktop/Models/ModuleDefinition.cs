using System.Collections.Generic;

namespace PaletixDesktop.Models
{
    public sealed class ModuleDefinition
    {
        public string Route { get; init; } = "";
        public string Title { get; init; } = "";
        public string Subtitle { get; init; } = "";
        public AppFeature Feature { get; init; }
        public IReadOnlyList<DashboardMetric> Metrics { get; init; } = new List<DashboardMetric>();
        public IReadOnlyList<ActivityItem> WorkItems { get; init; } = new List<ActivityItem>();
        public IReadOnlyList<string> PrimaryActions { get; init; } = new List<string>();
    }
}
