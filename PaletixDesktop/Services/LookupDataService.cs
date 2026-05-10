using PaletixDesktop.Models;
using SharedContracts.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PaletixDesktop.Services
{
    public sealed class LookupDataService
    {
        private const string ProductTypesCacheKey = "lookups/product-types/v1";
        private const string ProductsCacheKey = "lookups/products/v1";
        private const string SuppliersCacheKey = "lookups/suppliers/v1";
        private const string LocationsCacheKey = "lookups/locations/v1";
        private const string LotsCacheKey = "lookups/lots/v1";

        private readonly EntityApiService<TipusProducteReadDto, TipusProducteRequestDto> _productTypes;
        private readonly EntityApiService<ProductesReadDto, ProductesRequestDto> _products;
        private readonly EntityApiService<ProveidorsReadDto, ProveidorsRequestDto> _suppliers;
        private readonly EntityApiService<UbicacionsReadDto, UbicacionsRequestDto> _locations;
        private readonly EntityApiService<ProveidorsLotReadDto, ProveidorsLotRequestDto> _lots;
        private readonly LocalDatabase? _localDatabase;

        public LookupDataService(ApiClient apiClient, LocalDatabase? localDatabase = null)
        {
            _productTypes = new EntityApiService<TipusProducteReadDto, TipusProducteRequestDto>(apiClient, "api/TipusProductes");
            _products = new EntityApiService<ProductesReadDto, ProductesRequestDto>(apiClient, "api/Productes");
            _suppliers = new EntityApiService<ProveidorsReadDto, ProveidorsRequestDto>(apiClient, "api/Proveidors");
            _locations = new EntityApiService<UbicacionsReadDto, UbicacionsRequestDto>(apiClient, "api/Ubicacions");
            _lots = new EntityApiService<ProveidorsLotReadDto, ProveidorsLotRequestDto>(apiClient, "api/ProveidorsLots");
            _localDatabase = localDatabase;
        }

        public async Task<IReadOnlyList<LookupOption>> GetProductTypesAsync(bool includeEmpty, CancellationToken cancellationToken = default)
        {
            try
            {
                var items = await LoadAllAsync(_productTypes, item => new LookupOption
                {
                    Id = item.Id,
                    Label = string.IsNullOrWhiteSpace(item.DescripcioTipusProducte)
                        ? $"{item.TipusEnvas} / {item.EstatFisic}"
                        : item.DescripcioTipusProducte!
                }, cancellationToken);
                await SaveCacheAsync(ProductTypesCacheKey, items, cancellationToken);
                return WithEmpty(items, includeEmpty, "Sense tipus principal");
            }
            catch (Exception)
            {
                var cached = await ReadCacheAsync(ProductTypesCacheKey, cancellationToken);
                if (cached is not null)
                {
                    return WithEmpty(cached, includeEmpty, "Sense tipus principal");
                }

                return WithEmpty(DefaultProductTypes(), includeEmpty, "Sense tipus principal");
            }
        }

        public async Task<IReadOnlyList<LookupOption>> GetProductsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var items = await LoadAllAsync(_products, item => new LookupOption
                {
                    Id = item.Id,
                    Label = string.IsNullOrWhiteSpace(item.Referencia)
                        ? item.Nom
                        : $"{item.Referencia} · {item.Nom}"
                }, cancellationToken);
                await SaveCacheAsync(ProductsCacheKey, items, cancellationToken);
                return items;
            }
            catch (Exception)
            {
                var cached = await ReadCacheAsync(ProductsCacheKey, cancellationToken);
                if (cached is not null)
                {
                    return cached;
                }

                return DefaultProducts();
            }
        }

        public async Task<IReadOnlyList<LookupOption>> GetSuppliersAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var items = await LoadAllAsync(_suppliers, item => new LookupOption
                {
                    Id = item.Id,
                    Label = item.NomEmpresa
                }, cancellationToken);
                await SaveCacheAsync(SuppliersCacheKey, items, cancellationToken);
                return items;
            }
            catch (Exception)
            {
                var cached = await ReadCacheAsync(SuppliersCacheKey, cancellationToken);
                if (cached is not null)
                {
                    return cached;
                }

                return DefaultSuppliers();
            }
        }

        public async Task<IReadOnlyList<LookupOption>> GetLotsAsync(bool includeEmpty, CancellationToken cancellationToken = default)
        {
            try
            {
                var items = await LoadAllAsync(_lots, item => new LookupOption
                {
                    Id = item.Id,
                    Label = $"Producte {item.IdProducte}, proveidor {item.IdProveidor}, caduca {item.DataCaducitat:dd/MM/yyyy}"
                }, cancellationToken);
                await SaveCacheAsync(LotsCacheKey, items, cancellationToken);
                return WithEmpty(items, includeEmpty, "Sense lot");
            }
            catch (Exception)
            {
                var cached = await ReadCacheAsync(LotsCacheKey, cancellationToken);
                if (cached is not null)
                {
                    return WithEmpty(cached, includeEmpty, "Sense lot");
                }

                return WithEmpty(DefaultLots(), includeEmpty, "Sense lot");
            }
        }

        public async Task<IReadOnlyList<LookupOption>> GetLocationsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var items = await LoadAllAsync(_locations, item => new LookupOption
                {
                    Id = item.Id,
                    Label = string.IsNullOrWhiteSpace(item.CodiGenerat)
                        ? $"Zona {item.Zona}, passadis {item.Passadis}, bloc {item.BlocEstanteria}, fila {item.Fila}, columna {item.Columna}"
                        : item.CodiGenerat!
                }, cancellationToken);
                await SaveCacheAsync(LocationsCacheKey, items, cancellationToken);
                return items;
            }
            catch (Exception)
            {
                var cached = await ReadCacheAsync(LocationsCacheKey, cancellationToken);
                if (cached is not null)
                {
                    return cached;
                }

                return DefaultLocations();
            }
        }

        private async Task SaveCacheAsync(
            string key,
            IReadOnlyList<LookupOption> items,
            CancellationToken cancellationToken)
        {
            if (_localDatabase is null)
            {
                return;
            }

            await _localDatabase.InitializeAsync(cancellationToken);
            await _localDatabase.SetJsonAsync(key, items, cancellationToken);
        }

        private async Task<IReadOnlyList<LookupOption>?> ReadCacheAsync(
            string key,
            CancellationToken cancellationToken)
        {
            if (_localDatabase is null)
            {
                return null;
            }

            await _localDatabase.InitializeAsync(cancellationToken);
            return await _localDatabase.GetJsonAsync<List<LookupOption>>(key, cancellationToken);
        }

        private static async Task<IReadOnlyList<LookupOption>> LoadAllAsync<TRead, TRequest>(
            EntityApiService<TRead, TRequest> api,
            Func<TRead, LookupOption> map,
            CancellationToken cancellationToken)
        {
            var first = await api.GetPageAsync(1, 100, cancellationToken);
            var items = first.Items.Select(map).ToList();

            for (var page = 2; page <= first.TotalPages; page++)
            {
                var next = await api.GetPageAsync(page, 100, cancellationToken);
                items.AddRange(next.Items.Select(map));
            }

            return items;
        }

        private static IReadOnlyList<LookupOption> WithEmpty(
            IReadOnlyList<LookupOption> items,
            bool includeEmpty,
            string emptyLabel)
        {
            if (!includeEmpty)
            {
                return items;
            }

            return new[] { new LookupOption { Id = null, Label = emptyLabel } }
                .Concat(items)
                .ToList();
        }

        private static IReadOnlyList<LookupOption> DefaultProductTypes()
        {
            return new List<LookupOption>
            {
                new() { Id = 1, Label = "Begudes" },
                new() { Id = 2, Label = "Equipament" },
                new() { Id = 3, Label = "Consumible" },
                new() { Id = 4, Label = "IoT" },
                new() { Id = 5, Label = "Seguretat" }
            };
        }

        private static IReadOnlyList<LookupOption> DefaultSuppliers()
        {
            return new List<LookupOption>
            {
                new() { Id = 1, Label = "Distribucions Nord" },
                new() { Id = 2, Label = "LogiPack" },
                new() { Id = 3, Label = "PrintFlow" },
                new() { Id = 4, Label = "EmbalPro" },
                new() { Id = 5, Label = "ColdTrack" }
            };
        }

        private static IReadOnlyList<LookupOption> DefaultProducts()
        {
            return new List<LookupOption>
            {
                new() { Id = 1, Label = "Producte 1" },
                new() { Id = 2, Label = "Producte 2" },
                new() { Id = 3, Label = "Producte 3" }
            };
        }

        private static IReadOnlyList<LookupOption> DefaultLocations()
        {
            return new List<LookupOption>
            {
                new() { Id = 1, Label = "Zona 1, passadis 1" },
                new() { Id = 2, Label = "Zona 1, passadis 2" },
                new() { Id = 3, Label = "Zona 2, passadis 1" },
                new() { Id = 4, Label = "Zona 2, passadis 2" },
                new() { Id = 5, Label = "Zona 3, passadis 1" }
            };
        }

        private static IReadOnlyList<LookupOption> DefaultLots()
        {
            return new List<LookupOption>
            {
                new() { Id = 1, Label = "Lot 1" },
                new() { Id = 2, Label = "Lot 2" }
            };
        }
    }
}
