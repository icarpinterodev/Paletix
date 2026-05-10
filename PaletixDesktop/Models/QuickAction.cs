namespace PaletixDesktop.Models
{
    public sealed class QuickAction
    {
        public string Title { get; init; } = "";
        public string Detail { get; init; } = "";
        public string Route { get; init; } = "";
        public AppFeature Feature { get; init; }
    }
}
