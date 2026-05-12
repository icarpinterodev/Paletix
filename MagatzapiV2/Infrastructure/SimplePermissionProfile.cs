using MagatzapiV2.Models;

namespace MagatzapiV2.Infrastructure;

public static class SimplePermissionProfile
{
    public static List<string> Build(Usuaris user)
    {
        var role = Normalize(user.IdRolNavigation?.Nom);
        var job = Normalize(user.IdCarrecNavigation?.Nom);
        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "dashboard.view"
        };

        if (role.Contains("administrador") || role.Contains("admin"))
        {
            AddAll(permissions);
            return permissions.ToList();
        }

        if (job.Contains("cap") || job.Contains("responsable") || role.Contains("gestor"))
        {
            AddFeature(permissions, "operations");
            AddFeature(permissions, "warehouse");
            AddFeature(permissions, "catalog");
            AddFeature(permissions, "clients");
            AddFeature(permissions, "suppliers");
            AddFeature(permissions, "fleet");
            AddFeature(permissions, "gamification");
            return permissions.ToList();
        }

        if (job.Contains("preparador") || job.Contains("magatzem") || role.Contains("operari"))
        {
            AddFeature(permissions, "operations");
            AddFeature(permissions, "warehouse");
            AddFeature(permissions, "gamification");
            return permissions.ToList();
        }

        if (job.Contains("chofer") || job.Contains("transport"))
        {
            AddFeature(permissions, "operations");
            AddFeature(permissions, "fleet");
            AddFeature(permissions, "gamification");
            return permissions.ToList();
        }

        if (role.Contains("finances") || job.Contains("administracio"))
        {
            AddFeature(permissions, "billing");
            AddFeature(permissions, "clients");
            AddFeature(permissions, "suppliers");
        }

        return permissions.ToList();
    }

    private static void AddAll(HashSet<string> permissions)
    {
        foreach (var feature in new[] { "operations", "warehouse", "catalog", "clients", "suppliers", "fleet", "billing", "gamification", "users", "administration" })
        {
            AddFeature(permissions, feature);
        }
    }

    private static void AddFeature(HashSet<string> permissions, string feature)
    {
        foreach (var action in new[] { "view", "create", "edit", "delete", "approve", "assign", "sync" })
        {
            permissions.Add($"{feature}.{action}");
        }
    }

    private static string Normalize(string? value)
    {
        return (value ?? "").Trim().ToLowerInvariant();
    }
}
