namespace PaletixDesktop.Models
{
    public sealed class LookupOption
    {
        public int? Id { get; init; }
        public string Label { get; init; } = "";
        public string Value => Id?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "";
        public string DisplayText => Id is null ? Label : $"{Id} - {Label}";
    }
}
