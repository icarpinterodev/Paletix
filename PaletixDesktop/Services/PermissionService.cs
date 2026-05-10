using System;
using System.Collections.Generic;
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
    }
}
