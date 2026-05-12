using System.Collections.Generic;
using System.Linq;

namespace PaletixDesktop.Models
{
    public sealed class SessionUser
    {
        public int Id { get; init; }
        public string Name { get; init; } = "";
        public string Surnames { get; init; } = "";
        public string RoleName { get; init; } = "";
        public string JobTitle { get; init; } = "";
        public int Points { get; init; }
        public int Level { get; init; }
        public bool IsLocalSession { get; init; }
        public IReadOnlyList<string> Permissions { get; init; } = new List<string>();

        public string DisplayName => string.Join(" ", new[] { Name, Surnames }.Where(value => !string.IsNullOrWhiteSpace(value)));

        public string Initials
        {
            get
            {
                var parts = new[] { Name, Surnames }
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value.Trim()[0].ToString().ToUpperInvariant())
                    .Take(2)
                    .ToList();

                return parts.Count == 0 ? "U" : string.Concat(parts);
            }
        }
    }
}
