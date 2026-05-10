namespace PaletixDesktop.Models
{
    public sealed class ShellCommand
    {
        public string Id { get; init; } = "";
        public string Label { get; init; } = "";
        public string Glyph { get; init; } = "";
        public PermissionAction Action { get; init; } = PermissionAction.View;
    }
}
