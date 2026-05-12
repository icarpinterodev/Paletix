using System;
using System.Collections.Generic;
using System.Linq;
using PaletixDesktop.Models;

namespace PaletixDesktop.Services
{
    public sealed class PermissionService
    {
        private SessionUser? _currentUser;

        public void SetCurrentUser(SessionUser user)
        {
            _currentUser = user;
        }

        public bool CanAccess(AppFeature feature, PermissionAction action = PermissionAction.View)
        {
            if (feature == AppFeature.Dashboard)
            {
                return true;
            }

            if (_currentUser is null)
            {
                return false;
            }

            if (_currentUser.Permissions.Count > 0)
            {
                return _currentUser.Permissions.Contains(PermissionKey(feature, action), StringComparer.OrdinalIgnoreCase);
            }

            var role = Normalize(_currentUser.RoleName);
            var job = Normalize(_currentUser.JobTitle);

            if (role.Contains("administrador") || role.Contains("admin"))
            {
                return true;
            }

            if (job.Contains("cap") || job.Contains("responsable") || role.Contains("gestor"))
            {
                return feature is AppFeature.Operations
                    or AppFeature.Warehouse
                    or AppFeature.Catalog
                    or AppFeature.Clients
                    or AppFeature.Suppliers
                    or AppFeature.Fleet
                    or AppFeature.Gamification;
            }

            if (job.Contains("preparador") || job.Contains("magatzem") || role.Contains("operari"))
            {
                return feature is AppFeature.Operations
                    or AppFeature.Warehouse
                    or AppFeature.Gamification;
            }

            if (job.Contains("chofer") || job.Contains("transport"))
            {
                return feature is AppFeature.Operations
                    or AppFeature.Fleet
                    or AppFeature.Gamification;
            }

            if (role.Contains("finances") || job.Contains("administracio"))
            {
                return feature is AppFeature.Billing
                    or AppFeature.Clients
                    or AppFeature.Suppliers;
            }

            return false;
        }

        public IReadOnlyList<AppFeature> GetVisibleFeatures()
        {
            var visible = new List<AppFeature>();
            foreach (AppFeature feature in Enum.GetValues(typeof(AppFeature)))
            {
                if (CanAccess(feature))
                {
                    visible.Add(feature);
                }
            }

            return visible;
        }

        private static string Normalize(string value)
        {
            return value.Trim().ToLowerInvariant();
        }

        private static string PermissionKey(AppFeature feature, PermissionAction action)
        {
            return $"{FeatureKey(feature)}.{action.ToString().ToLowerInvariant()}";
        }

        private static string FeatureKey(AppFeature feature)
        {
            return feature switch
            {
                AppFeature.Dashboard => "dashboard",
                AppFeature.Operations => "operations",
                AppFeature.Warehouse => "warehouse",
                AppFeature.Catalog => "catalog",
                AppFeature.Clients => "clients",
                AppFeature.Suppliers => "suppliers",
                AppFeature.Fleet => "fleet",
                AppFeature.Billing => "billing",
                AppFeature.Gamification => "gamification",
                AppFeature.Users => "users",
                AppFeature.Administration => "administration",
                _ => feature.ToString().ToLowerInvariant()
            };
        }
    }
}
