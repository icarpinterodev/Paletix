using System.Collections.Generic;
using PaletixDesktop.Models;

namespace PaletixDesktop.Services
{
    public sealed class ModuleCatalog
    {
        private readonly Dictionary<string, ModuleDefinition> _modules = new()
        {
            ["operations"] = new ModuleDefinition
            {
                Route = "operations",
                Title = "Operacions",
                Subtitle = "Comandes, preparacio i entregues del dia",
                Feature = AppFeature.Operations,
                Metrics = new[]
                {
                    new DashboardMetric { Title = "Pendents", Value = "18", Detail = "5 prioritaries", State = "Atencio" },
                    new DashboardMetric { Title = "En preparacio", Value = "11", Detail = "3 equips actius", State = "Actiu" },
                    new DashboardMetric { Title = "Entregues", Value = "24", Detail = "Ruta optimitzada", State = "Correcte" }
                },
                WorkItems = new[]
                {
                    new ActivityItem { Title = "COM-1042", Detail = "Preparacio parcial, falta validar ubicacio", Time = "08:40", State = "Bloqueig" },
                    new ActivityItem { Title = "COM-1045", Detail = "Assignada a vehicle electric", Time = "09:10", State = "En curs" },
                    new ActivityItem { Title = "COM-1049", Detail = "Llista per facturar", Time = "10:05", State = "Completada" }
                },
                PrimaryActions = new[] { "Nova comanda", "Assignar ruta", "Validar entrega" }
            },
            ["warehouse"] = new ModuleDefinition
            {
                Route = "warehouse",
                Title = "Magatzem",
                Subtitle = "Stock, lots, ubicacions i reposicio",
                Feature = AppFeature.Warehouse,
                Metrics = new[]
                {
                    new DashboardMetric { Title = "Stock disponible", Value = "84%", Detail = "12 alertes baixes", State = "Actiu" },
                    new DashboardMetric { Title = "Ubicacions", Value = "326", Detail = "19 lliures", State = "Correcte" },
                    new DashboardMetric { Title = "Incidencies", Value = "4", Detail = "2 greus", State = "Atencio" }
                },
                WorkItems = new[]
                {
                    new ActivityItem { Title = "Z2P4-E3", Detail = "Recompte pendent de productes fragils", Time = "07:55", State = "Pendent" },
                    new ActivityItem { Title = "Lot PV-778", Detail = "Entrada pendent de qualitat", Time = "08:25", State = "En curs" },
                    new ActivityItem { Title = "SKU-1440", Detail = "Reposicio recomanada", Time = "09:30", State = "Atencio" }
                },
                PrimaryActions = new[] { "Escanejar ubicacio", "Ajustar stock", "Crear lot" }
            },
            ["catalog"] = new ModuleDefinition
            {
                Route = "catalog",
                Title = "Productes",
                Subtitle = "Cataleg, preus, proveidors i tipus de producte",
                Feature = AppFeature.Catalog,
                Metrics = new[]
                {
                    new DashboardMetric { Title = "Actius", Value = "412", Detail = "23 nous aquest mes", State = "Correcte" },
                    new DashboardMetric { Title = "Marge mitja", Value = "31%", Detail = "2 punts sobre objectiu", State = "Correcte" },
                    new DashboardMetric { Title = "Sense imatge", Value = "17", Detail = "Millorable per picking", State = "Atencio" }
                },
                WorkItems = new[]
                {
                    new ActivityItem { Title = "SKU-2018", Detail = "Cost actualitzat pel proveidor", Time = "Ahir", State = "Revisio" },
                    new ActivityItem { Title = "Begudes isotoniques", Detail = "Tipus pendent de normalitzar", Time = "Ahir", State = "Pendent" }
                },
                PrimaryActions = new[] { "Nou producte", "Editar preus", "Importar cataleg" }
            },
            ["clients"] = new ModuleDefinition
            {
                Route = "clients",
                Title = "Clients",
                Subtitle = "Fitxes comercials, adreces i historial de comandes",
                Feature = AppFeature.Clients,
                Metrics = new[]
                {
                    new DashboardMetric { Title = "Clients actius", Value = "96", Detail = "8 amb ruta fixa", State = "Correcte" },
                    new DashboardMetric { Title = "Comandes mes", Value = "286", Detail = "14% mes que el mes anterior", State = "Actiu" },
                    new DashboardMetric { Title = "Incidencies", Value = "3", Detail = "Pendents de resposta", State = "Atencio" }
                },
                WorkItems = new[]
                {
                    new ActivityItem { Title = "Distribucions Nord", Detail = "Nova adreca d'entrega", Time = "10:15", State = "Nou" },
                    new ActivityItem { Title = "Hotel Miramar", Detail = "Revisar condicions de facturacio", Time = "Ahir", State = "Revisio" }
                },
                PrimaryActions = new[] { "Nou client", "Veure historial", "Exportar llistat" }
            },
            ["suppliers"] = new ModuleDefinition
            {
                Route = "suppliers",
                Title = "Proveidors",
                Subtitle = "Compres, lots rebuts i rendiment de proveidors",
                Feature = AppFeature.Suppliers,
                Metrics = new[]
                {
                    new DashboardMetric { Title = "Proveidors", Value = "38", Detail = "6 prioritaris", State = "Correcte" },
                    new DashboardMetric { Title = "Lots pendents", Value = "9", Detail = "3 arriben avui", State = "Actiu" },
                    new DashboardMetric { Title = "Retards", Value = "2", Detail = "Afecten stock critic", State = "Atencio" }
                },
                WorkItems = new[]
                {
                    new ActivityItem { Title = "PV-332", Detail = "Confirmar data d'arribada", Time = "09:00", State = "Pendent" },
                    new ActivityItem { Title = "Lot PV-780", Detail = "Documentacio incompleta", Time = "09:45", State = "Atencio" }
                },
                PrimaryActions = new[] { "Nou proveidor", "Registrar lot", "Comparar rendiment" }
            },
            ["fleet"] = new ModuleDefinition
            {
                Route = "fleet",
                Title = "Flota",
                Subtitle = "Vehicles, rutes i disponibilitat de transport",
                Feature = AppFeature.Fleet,
                Metrics = new[]
                {
                    new DashboardMetric { Title = "Vehicles lliures", Value = "7", Detail = "2 electrics", State = "Correcte" },
                    new DashboardMetric { Title = "En ruta", Value = "12", Detail = "1 amb retard", State = "Actiu" },
                    new DashboardMetric { Title = "Manteniment", Value = "3", Detail = "1 urgent", State = "Atencio" }
                },
                WorkItems = new[]
                {
                    new ActivityItem { Title = "VEH-014", Detail = "Kilometratge pendent de registre", Time = "08:05", State = "Pendent" },
                    new ActivityItem { Title = "Ruta B-12", Detail = "Canvi de xofer recomanat", Time = "08:55", State = "Revisio" }
                },
                PrimaryActions = new[] { "Assignar vehicle", "Planificar ruta", "Registrar manteniment" }
            },
            ["billing"] = new ModuleDefinition
            {
                Route = "billing",
                Title = "Facturacio",
                Subtitle = "Factures, pagaments i estat economic de comandes",
                Feature = AppFeature.Billing,
                Metrics = new[]
                {
                    new DashboardMetric { Title = "Factures obertes", Value = "41", Detail = "12 vencen aquesta setmana", State = "Atencio" },
                    new DashboardMetric { Title = "Cobrat mes", Value = "82K", Detail = "Objectiu al 76%", State = "Actiu" },
                    new DashboardMetric { Title = "Pagaments", Value = "29", Detail = "Sense errors", State = "Correcte" }
                },
                WorkItems = new[]
                {
                    new ActivityItem { Title = "FAC-2026-118", Detail = "Pagament parcial rebut", Time = "09:20", State = "En curs" },
                    new ActivityItem { Title = "FAC-2026-121", Detail = "Pendent d'emissio", Time = "10:00", State = "Pendent" }
                },
                PrimaryActions = new[] { "Emetre factura", "Registrar pagament", "Exportar resum" }
            },
            ["gamification"] = new ModuleDefinition
            {
                Route = "gamification",
                Title = "Gamificacio",
                Subtitle = "Punts, nivells, reptes, medalles i premis",
                Feature = AppFeature.Gamification,
                Metrics = new[]
                {
                    new DashboardMetric { Title = "Reptes actius", Value = "6", Detail = "2 acaben avui", State = "Actiu" },
                    new DashboardMetric { Title = "Punts repartits", Value = "12.4K", Detail = "Mes actual", State = "Correcte" },
                    new DashboardMetric { Title = "Bescanvis", Value = "18", Detail = "3 pendents d'aprovar", State = "Atencio" }
                },
                WorkItems = new[]
                {
                    new ActivityItem { Title = "Repte zero errors", Detail = "Equip de tarda al 88%", Time = "Avui", State = "Actiu" },
                    new ActivityItem { Title = "Premi extra descans", Detail = "Stock pendent de confirmar", Time = "Ahir", State = "Revisio" }
                },
                PrimaryActions = new[] { "Crear repte", "Assignar medalla", "Aprovar premi" }
            },
            ["users"] = new ModuleDefinition
            {
                Route = "users",
                Title = "Usuaris",
                Subtitle = "Treballadors, rols, carrecs i rendiment",
                Feature = AppFeature.Users,
                Metrics = new[]
                {
                    new DashboardMetric { Title = "Treballadors", Value = "54", Detail = "8 torn de nit", State = "Correcte" },
                    new DashboardMetric { Title = "Rols", Value = "5", Detail = "Permisos revisats", State = "Correcte" },
                    new DashboardMetric { Title = "Stats pendents", Value = "4", Detail = "Cal sincronitzar", State = "Atencio" }
                },
                WorkItems = new[]
                {
                    new ActivityItem { Title = "Alta usuari", Detail = "Nou preparador pendent de rol", Time = "08:10", State = "Pendent" },
                    new ActivityItem { Title = "Revisio permisos", Detail = "Administracio demana acces a factures", Time = "Ahir", State = "Revisio" }
                },
                PrimaryActions = new[] { "Nou usuari", "Editar permisos", "Veure estadistiques" }
            },
            ["admin"] = new ModuleDefinition
            {
                Route = "admin",
                Title = "Administracio",
                Subtitle = "Configuracio, API, sincronitzacio i diagnosi",
                Feature = AppFeature.Administration,
                Metrics = new[]
                {
                    new DashboardMetric { Title = "API", Value = "Local", Detail = "https://localhost:7137", State = "Configurat" },
                    new DashboardMetric { Title = "SQLite", Value = "Actiu", Detail = "Cache i outbox preparats", State = "Correcte" },
                    new DashboardMetric { Title = "Permisos", Value = "Rol + carrec", Detail = "Aplicats a la navegacio", State = "Correcte" }
                },
                WorkItems = new[]
                {
                    new ActivityItem { Title = "Clau API", Detail = "Configurable amb PALETIX_API_KEY", Time = "Sistema", State = "Config" },
                    new ActivityItem { Title = "Base local", Detail = "Preparada per sincronitzacio incremental", Time = "Sistema", State = "Actiu" }
                },
                PrimaryActions = new[] { "Provar API", "Sincronitzar", "Veure logs" }
            }
        };

        public ModuleDefinition GetModule(string route)
        {
            var moduleRoute = ResolveModuleRoute(route);
            return _modules.TryGetValue(moduleRoute, out var module)
                ? module
                : _modules["operations"];
        }

        private static string ResolveModuleRoute(string route)
        {
            if (route.StartsWith("operations-"))
            {
                return "operations";
            }

            if (route.StartsWith("warehouse-"))
            {
                return "warehouse";
            }

            if (route == "commercial-products")
            {
                return "catalog";
            }

            if (route == "commercial-clients")
            {
                return "clients";
            }

            if (route == "commercial-suppliers")
            {
                return "suppliers";
            }

            if (route.StartsWith("fleet-"))
            {
                return "fleet";
            }

            if (route.StartsWith("finance-"))
            {
                return "billing";
            }

            if (route.StartsWith("game-"))
            {
                return "gamification";
            }

            if (route == "admin-users")
            {
                return "users";
            }

            if (route == "admin-sync")
            {
                return "admin";
            }

            return route;
        }
    }
}
