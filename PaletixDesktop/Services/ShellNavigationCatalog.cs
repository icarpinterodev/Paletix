using System.Collections.Generic;
using System.Linq;
using PaletixDesktop.Models;

namespace PaletixDesktop.Services
{
    public sealed class ShellNavigationCatalog
    {
        private readonly IReadOnlyList<ShellCategory> _categories = new List<ShellCategory>
        {
            new()
            {
                Id = "home",
                Title = "Inici",
                Glyph = "\uE80F",
                Sections = new[]
                {
                    Section("dashboard", "Resum executiu", "Indicadors, alertes i accessos rapids", "\uE9D2", AppFeature.Dashboard,
                        Command("Actualitzar", "\uE72C", PermissionAction.View),
                        Command("Sincronitzar", "\uE895", PermissionAction.Sync))
                }
            },
            new()
            {
                Id = "operations",
                Title = "Operacions",
                Glyph = "\uE8FD",
                Sections = new[]
                {
                    Section("operations-orders", "Comandes", "Creacio, estat i seguiment de comandes", "\uE8FD", AppFeature.Operations,
                        Command("Nova comanda", "\uE710", PermissionAction.Create, "orders.create"),
                        Command("Actualitzar", "\uE72C", PermissionAction.View, "orders.refresh"),
                        Command("Editar", "\uE70F", PermissionAction.Edit, "orders.edit"),
                        Command("Eliminar", "\uE74D", PermissionAction.Delete, "orders.delete")),
                    Section("operations-picking", "Preparacio", "Picking, verificacio i incidencies", "\uE7C3", AppFeature.Operations,
                        Command("Actualitzar", "\uE72C", PermissionAction.View, "picking.refresh"),
                        Command("Iniciar", "\uE768", PermissionAction.Edit, "picking.start"),
                        Command("Pausar", "\uE769", PermissionAction.Edit, "picking.pause"),
                        Command("Preparat", "\uE73E", PermissionAction.Edit, "picking.prepared"),
                        Command("Incidencia", "\uE7BA", PermissionAction.Create, "picking.incident")),
                    Section("operations-routes", "Rutes", "Assignacio de vehicle, xofer i entregues", "\uE707", AppFeature.Operations,
                        Command("Planificar", "\uE8A5", PermissionAction.Assign),
                        Command("Optimitzar", "\uE74E", PermissionAction.Edit))
                }
            },
            new()
            {
                Id = "warehouse",
                Title = "Magatzem",
                Glyph = "\uE719",
                Sections = new[]
                {
                    Section("warehouse-stock", "Stock", "Existencies, reserves i alertes", "\uE8F1", AppFeature.Warehouse,
                        Command("Entrada", "\uE710", PermissionAction.Create, "stock.entry"),
                        Command("Moure", "\uE8AB", PermissionAction.Edit, "stock.move"),
                        Command("Ajust", "\uE7C9", PermissionAction.Edit, "stock.adjust"),
                        Command("Reservar", "\uE72E", PermissionAction.Edit, "stock.reserve"),
                        Command("Alliberar", "\uE785", PermissionAction.Edit, "stock.release"),
                        Command("Historial", "\uE81C", PermissionAction.View, "stock.history"),
                        Command("Actualitzar", "\uE72C", PermissionAction.View, "stock.refresh"),
                        Command("Taula", "\uE80A", PermissionAction.View, "stock.view.table"),
                        Command("Grid", "\uE8A9", PermissionAction.View, "stock.view.grid")),
                    Section("warehouse-locations", "Ubicacions", "Zones, passadissos i posicions", "\uE707", AppFeature.Warehouse,
                        Command("Nova ubicacio", "\uE710", PermissionAction.Create, "locations.create"),
                        Command("Generar", "\uE8B5", PermissionAction.Create, "locations.generate"),
                        Command("Actualitzar", "\uE72C", PermissionAction.View, "locations.refresh"),
                        Command("Editar", "\uE70F", PermissionAction.Edit, "locations.edit"),
                        Command("Eliminar", "\uE74D", PermissionAction.Delete, "locations.delete"),
                        Command("Taula", "\uE80A", PermissionAction.View, "locations.view.table"),
                        Command("Dissenyador", "\uECA5", PermissionAction.View, "locations.view.designer")),
                    Section("warehouse-lots", "Lots", "Entrades, caducitats i proveidor", "\uE7B8", AppFeature.Warehouse,
                        Command("Registrar lot", "\uE710", PermissionAction.Create, "lots.create"),
                        Command("Actualitzar", "\uE72C", PermissionAction.View, "lots.refresh"),
                        Command("Editar", "\uE70F", PermissionAction.Edit, "lots.edit"),
                        Command("Eliminar", "\uE74D", PermissionAction.Delete, "lots.delete"),
                        Command("Taula", "\uE80A", PermissionAction.View, "lots.view.table"),
                        Command("Grid", "\uE8A9", PermissionAction.View, "lots.view.grid"))
                }
            },
            new()
            {
                Id = "commercial",
                Title = "Comercial",
                Glyph = "\uE716",
                Sections = new[]
                {
                    Section("commercial-products", "Productes", "Cataleg, preus i tipus", "\uE8F1", AppFeature.Catalog,
                        Command("Nou producte", "\uE710", PermissionAction.Create, "products.create"),
                        Command("Actualitzar", "\uE72C", PermissionAction.View, "products.refresh"),
                        Command("Editar", "\uE70F", PermissionAction.Edit, "products.edit"),
                        Command("Eliminar", "\uE74D", PermissionAction.Delete, "products.delete"),
                        Command("Importar", "\uE8B5", PermissionAction.Create, "products.import"),
                        Command("Taula", "\uE80A", PermissionAction.View, "products.view.table"),
                        Command("Grid", "\uE8A9", PermissionAction.View, "products.view.grid")),
                    Section("commercial-clients", "Clients", "Fitxes, adreces i historial", "\uE716", AppFeature.Clients,
                        Command("Nou client", "\uE710", PermissionAction.Create, "clients.create"),
                        Command("Actualitzar", "\uE72C", PermissionAction.View, "clients.refresh"),
                        Command("Editar", "\uE70F", PermissionAction.Edit, "clients.edit"),
                        Command("Eliminar", "\uE74D", PermissionAction.Delete, "clients.delete"),
                        Command("Historial", "\uE81C", PermissionAction.View, "clients.history"),
                        Command("Taula", "\uE80A", PermissionAction.View, "clients.view.table"),
                        Command("Grid", "\uE8A9", PermissionAction.View, "clients.view.grid")),
                    Section("commercial-suppliers", "Proveidors", "Acords, rendiment i lots rebuts", "\uE77B", AppFeature.Suppliers,
                        Command("Nou proveidor", "\uE710", PermissionAction.Create, "suppliers.create"),
                        Command("Actualitzar", "\uE72C", PermissionAction.View, "suppliers.refresh"),
                        Command("Editar", "\uE70F", PermissionAction.Edit, "suppliers.edit"),
                        Command("Eliminar", "\uE74D", PermissionAction.Delete, "suppliers.delete"),
                        Command("Comparar", "\uE9D2", PermissionAction.View, "suppliers.compare"),
                        Command("Taula", "\uE80A", PermissionAction.View, "suppliers.view.table"),
                        Command("Grid", "\uE8A9", PermissionAction.View, "suppliers.view.grid"))
                }
            },
            new()
            {
                Id = "fleet",
                Title = "Flota",
                Glyph = "\uE707",
                Sections = new[]
                {
                    Section("fleet-vehicles", "Vehicles", "Disponibilitat, tipus i manteniment", "\uE804", AppFeature.Fleet,
                        Command("Assignar", "\uE8D4", PermissionAction.Assign),
                        Command("Manteniment", "\uE90F", PermissionAction.Edit)),
                    Section("fleet-drivers", "Xofers", "Assignacions, torns i rendiment", "\uE77B", AppFeature.Fleet,
                        Command("Canviar xofer", "\uE8D4", PermissionAction.Assign))
                }
            },
            new()
            {
                Id = "finance",
                Title = "Finances",
                Glyph = "\uE8EF",
                Sections = new[]
                {
                    Section("finance-invoices", "Factures", "Emissio, estat i venciments", "\uE9D9", AppFeature.Billing,
                        Command("Emetre", "\uE710", PermissionAction.Create),
                        Command("Marcar pagada", "\uE73E", PermissionAction.Edit)),
                    Section("finance-payments", "Pagaments", "Cobraments i conciliacio", "\uE8C7", AppFeature.Billing,
                        Command("Registrar", "\uE710", PermissionAction.Create),
                        Command("Exportar", "\uE896", PermissionAction.View))
                }
            },
            new()
            {
                Id = "gamification",
                Title = "Gamificacio",
                Glyph = "\uE734",
                Sections = new[]
                {
                    Section("game-challenges", "Reptes", "Objectius, punts i participants", "\uE9D9", AppFeature.Gamification,
                        Command("Crear repte", "\uE710", PermissionAction.Create),
                        Command("Tancar repte", "\uE73E", PermissionAction.Approve)),
                    Section("game-awards", "Premis", "Medalles, bescanvis i recompensa", "\uE734", AppFeature.Gamification,
                        Command("Assignar medalla", "\uE8D4", PermissionAction.Assign),
                        Command("Aprovar premi", "\uE73E", PermissionAction.Approve))
                }
            },
            new()
            {
                Id = "administration",
                Title = "Admin",
                Glyph = "\uE713",
                Sections = new[]
                {
                    Section("admin-users", "Usuaris i rols", "Treballadors, carrecs i permisos", "\uE7EF", AppFeature.Users,
                        Command("Nou", "\uE710", PermissionAction.Create, "admin-users.create"),
                        Command("Actualitzar", "\uE72C", PermissionAction.View, "admin-users.refresh"),
                        Command("Editar", "\uE70F", PermissionAction.Edit, "admin-users.edit"),
                        Command("Eliminar", "\uE74D", PermissionAction.Delete, "admin-users.delete")),
                    Section("admin-sync", "API i sincronitzacio", "Configuracio, SQLite i cua offline", "\uE895", AppFeature.Administration,
                        Command("Provar API", "\uE9D9", PermissionAction.View),
                        Command("Sincronitzar", "\uE895", PermissionAction.Sync))
                }
            }
        };

        public IReadOnlyList<ShellCategory> GetVisibleCategories(PermissionService permissions)
        {
            return _categories
                .Select(category => new ShellCategory
                {
                    Id = category.Id,
                    Title = category.Title,
                    Glyph = category.Glyph,
                    Sections = category.Sections
                        .Where(section => permissions.CanAccess(section.Feature))
                        .ToList()
                })
                .Where(category => category.Sections.Count > 0)
                .ToList();
        }

        private static ShellSection Section(
            string route,
            string title,
            string subtitle,
            string glyph,
            AppFeature feature,
            params ShellCommand[] commands)
        {
            return new ShellSection
            {
                Route = route,
                Title = title,
                Subtitle = subtitle,
                Glyph = glyph,
                Feature = feature,
                Commands = commands
            };
        }

        private static ShellCommand Command(string label, string glyph, PermissionAction action, string? id = null)
        {
            return new ShellCommand
            {
                Id = id ?? label.Trim().ToLowerInvariant().Replace(" ", "."),
                Label = label,
                Glyph = glyph,
                Action = action
            };
        }
    }
}
