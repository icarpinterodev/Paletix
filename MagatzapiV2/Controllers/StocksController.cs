using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MagatzapiV2.Data;
using MagatzapiV2.Dtos;
using SharedContracts.Dtos;
using SharedContracts;
using MagatzapiV2.Infrastructure;
using MagatzapiV2.Models;

namespace MagatzapiV2.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class StocksController : ControllerBase
{
    private readonly AppDbContext _context;

    public StocksController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<StockReadDto>>> GetStock([FromQuery] PaginationQuery query)
    {
        var source = _context.Stock.AsNoTracking().OrderBy(entity => entity.Id);
        var totalItems = await source.CountAsync();
        var entities = await source
            .Skip(query.Skip)
            .Take(query.PageSize)
            .ToListAsync();
        var items = entities.Select(EntityMappings.ToReadDto).ToList();

        return Ok(PagedResult<StockReadDto>.Create(items, query, totalItems));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<StockReadDto>> GetStock(int id)
    {
        var entity = await _context.Stock
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == id);

        if (entity == null)
        {
            return NotFound();
        }

        return Ok(EntityMappings.ToReadDto(entity));
    }

    [HttpPost]
    public async Task<ActionResult<StockReadDto>> PostStock(StockRequestDto dto)
    {
        var entity = EntityMappings.ToEntity(dto);
        _context.Stock.Add(entity);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            return this.DatabaseConflict(exception);
        }

        return CreatedAtAction(nameof(GetStock), new { id = entity.Id }, EntityMappings.ToReadDto(entity));
    }

    [HttpPost("entrada")]
    public async Task<ActionResult<StockOperationResultDto>> Entrada(StockEntradaRequestDto dto)
    {
        if (dto.Quantitat <= 0)
        {
            return BadRequest("La quantitat ha de ser superior a 0.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        var stock = await FindStockAsync(dto.IdProducte, dto.IdUbicacio, dto.IdLot);
        var totalBefore = stock?.TotalsEnStock ?? 0;
        var reservedBefore = stock?.ReservatsPerComandes ?? 0;

        if (stock is null)
        {
            stock = new Stock
            {
                IdProducte = dto.IdProducte,
                IdUbicacio = dto.IdUbicacio,
                IdLot = dto.IdLot,
                TotalsEnStock = dto.Quantitat,
                ReservatsPerComandes = 0
            };
            _context.Stock.Add(stock);
        }
        else
        {
            stock.TotalsEnStock += dto.Quantitat;
        }

        var moviment = NewMoviment(
            "Entrada",
            dto.IdProducte,
            dto.IdLot,
            null,
            dto.IdUbicacio,
            dto.Quantitat,
            null,
            null,
            null,
            null,
            totalBefore,
            stock.TotalsEnStock,
            reservedBefore,
            stock.ReservatsPerComandes,
            dto.Motiu);
        _context.StockMoviments.Add(moviment);

        await SaveOperationAsync(transaction);
        return Ok(new StockOperationResultDto
        {
            Stock = EntityMappings.ToReadDto(await ReloadStockAsync(stock.Id)),
            Moviment = EntityMappings.ToReadDto(moviment)
        });
    }

    [HttpPost("moure")]
    public async Task<ActionResult<StockOperationResultDto>> Moure(StockMovimentRequestDto dto)
    {
        if (dto.Quantitat <= 0)
        {
            return BadRequest("La quantitat ha de ser superior a 0.");
        }

        if (dto.IdUbicacioOrigen == dto.IdUbicacioDesti)
        {
            return BadRequest("La ubicacio origen i desti han de ser diferents.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        var origen = await FindStockAsync(dto.IdProducte, dto.IdUbicacioOrigen, dto.IdLot);
        if (origen is null)
        {
            return NotFound("No existeix stock a la ubicacio origen.");
        }

        var disponible = Available(origen);
        if (disponible < dto.Quantitat)
        {
            return BadRequest("No hi ha prou stock disponible per moure.");
        }

        var desti = await FindStockAsync(dto.IdProducte, dto.IdUbicacioDesti, dto.IdLot);
        var origenTotalBefore = origen.TotalsEnStock;
        var origenReservedBefore = origen.ReservatsPerComandes;
        var destiTotalBefore = desti?.TotalsEnStock ?? 0;
        var destiReservedBefore = desti?.ReservatsPerComandes ?? 0;

        origen.TotalsEnStock -= dto.Quantitat;
        if (desti is null)
        {
            desti = new Stock
            {
                IdProducte = dto.IdProducte,
                IdUbicacio = dto.IdUbicacioDesti,
                IdLot = dto.IdLot,
                TotalsEnStock = dto.Quantitat,
                ReservatsPerComandes = 0
            };
            _context.Stock.Add(desti);
        }
        else
        {
            desti.TotalsEnStock += dto.Quantitat;
        }

        var moviment = NewMoviment(
            "Moviment",
            dto.IdProducte,
            dto.IdLot,
            dto.IdUbicacioOrigen,
            dto.IdUbicacioDesti,
            dto.Quantitat,
            origenTotalBefore,
            origen.TotalsEnStock,
            origenReservedBefore,
            origen.ReservatsPerComandes,
            destiTotalBefore,
            desti.TotalsEnStock,
            destiReservedBefore,
            desti.ReservatsPerComandes,
            dto.Motiu);
        _context.StockMoviments.Add(moviment);

        await SaveOperationAsync(transaction);
        return Ok(new StockOperationResultDto
        {
            Origen = EntityMappings.ToReadDto(await ReloadStockAsync(origen.Id)),
            Desti = EntityMappings.ToReadDto(await ReloadStockAsync(desti.Id)),
            Moviment = EntityMappings.ToReadDto(moviment)
        });
    }

    [HttpPost("ajust")]
    public async Task<ActionResult<StockOperationResultDto>> Ajust(StockAjustRequestDto dto)
    {
        if (dto.NouTotal < 0)
        {
            return BadRequest("El nou total no pot ser negatiu.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        var stock = await _context.Stock.FirstOrDefaultAsync(entity => entity.Id == dto.IdStock);
        if (stock is null)
        {
            return NotFound();
        }

        if (dto.NouTotal < stock.ReservatsPerComandes)
        {
            return BadRequest("El total no pot ser inferior al stock reservat.");
        }

        var totalBefore = stock.TotalsEnStock;
        var reservedBefore = stock.ReservatsPerComandes;
        stock.TotalsEnStock = dto.NouTotal;

        var moviment = NewMoviment(
            "Ajust",
            stock.IdProducte,
            stock.IdLot,
            stock.IdUbicacio,
            stock.IdUbicacio,
            Math.Abs(dto.NouTotal - totalBefore),
            totalBefore,
            stock.TotalsEnStock,
            reservedBefore,
            stock.ReservatsPerComandes,
            null,
            null,
            null,
            null,
            dto.Motiu);
        _context.StockMoviments.Add(moviment);

        await SaveOperationAsync(transaction);
        return Ok(new StockOperationResultDto
        {
            Stock = EntityMappings.ToReadDto(await ReloadStockAsync(stock.Id)),
            Moviment = EntityMappings.ToReadDto(moviment)
        });
    }

    [HttpPost("reservar")]
    public async Task<ActionResult<StockOperationResultDto>> Reservar(StockReservaRequestDto dto)
    {
        if (dto.Quantitat <= 0)
        {
            return BadRequest("La quantitat ha de ser superior a 0.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        var stock = await _context.Stock.FirstOrDefaultAsync(entity => entity.Id == dto.IdStock);
        if (stock is null)
        {
            return NotFound();
        }

        if (Available(stock) < dto.Quantitat)
        {
            return BadRequest("No hi ha prou stock disponible per reservar.");
        }

        var totalBefore = stock.TotalsEnStock;
        var reservedBefore = stock.ReservatsPerComandes;
        stock.ReservatsPerComandes += dto.Quantitat;

        var moviment = NewMoviment(
            "Reserva",
            stock.IdProducte,
            stock.IdLot,
            stock.IdUbicacio,
            stock.IdUbicacio,
            dto.Quantitat,
            totalBefore,
            stock.TotalsEnStock,
            reservedBefore,
            stock.ReservatsPerComandes,
            null,
            null,
            null,
            null,
            dto.Motiu);
        _context.StockMoviments.Add(moviment);

        await SaveOperationAsync(transaction);
        return Ok(new StockOperationResultDto
        {
            Stock = EntityMappings.ToReadDto(await ReloadStockAsync(stock.Id)),
            Moviment = EntityMappings.ToReadDto(moviment)
        });
    }

    [HttpPost("alliberar")]
    public async Task<ActionResult<StockOperationResultDto>> Alliberar(StockAlliberamentRequestDto dto)
    {
        if (dto.Quantitat <= 0)
        {
            return BadRequest("La quantitat ha de ser superior a 0.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        var stock = await _context.Stock.FirstOrDefaultAsync(entity => entity.Id == dto.IdStock);
        if (stock is null)
        {
            return NotFound();
        }

        if (stock.ReservatsPerComandes < dto.Quantitat)
        {
            return BadRequest("No es pot alliberar mes stock del reservat.");
        }

        var totalBefore = stock.TotalsEnStock;
        var reservedBefore = stock.ReservatsPerComandes;
        stock.ReservatsPerComandes -= dto.Quantitat;

        var moviment = NewMoviment(
            "Alliberament",
            stock.IdProducte,
            stock.IdLot,
            stock.IdUbicacio,
            stock.IdUbicacio,
            dto.Quantitat,
            totalBefore,
            stock.TotalsEnStock,
            reservedBefore,
            stock.ReservatsPerComandes,
            null,
            null,
            null,
            null,
            dto.Motiu);
        _context.StockMoviments.Add(moviment);

        await SaveOperationAsync(transaction);
        return Ok(new StockOperationResultDto
        {
            Stock = EntityMappings.ToReadDto(await ReloadStockAsync(stock.Id)),
            Moviment = EntityMappings.ToReadDto(moviment)
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutStock(int id, StockRequestDto dto)
    {
        var entity = await _context.Stock.FirstOrDefaultAsync(entity => entity.Id == id);
        if (entity == null)
        {
            return NotFound();
        }

        EntityMappings.ApplyUpdate(dto, entity);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            return this.DatabaseConflict(exception);
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteStock(int id)
    {
        var entity = await _context.Stock.FirstOrDefaultAsync(entity => entity.Id == id);
        if (entity == null)
        {
            return NotFound();
        }

        _context.Stock.Remove(entity);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            return this.DatabaseConflict(exception);
        }

        return NoContent();
    }

    private Task<Stock?> FindStockAsync(int idProducte, int idUbicacio, int? idLot)
    {
        return _context.Stock.FirstOrDefaultAsync(entity =>
            entity.IdProducte == idProducte
            && entity.IdUbicacio == idUbicacio
            && entity.IdLot == idLot);
    }

    private async Task<Stock> ReloadStockAsync(int id)
    {
        return await _context.Stock.AsNoTracking().FirstAsync(entity => entity.Id == id);
    }

    private async Task SaveOperationAsync(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction)
    {
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static int Available(Stock stock)
    {
        return StockOperationRules.Available(stock.TotalsEnStock, stock.ReservatsPerComandes);
    }

    private static StockMoviments NewMoviment(
        string tipus,
        int idProducte,
        int? idLot,
        int? idUbicacioOrigen,
        int? idUbicacioDesti,
        int quantitat,
        int? totalOrigenAbans,
        int? totalOrigenDespres,
        int? reservatOrigenAbans,
        int? reservatOrigenDespres,
        int? totalDestiAbans,
        int? totalDestiDespres,
        int? reservatDestiAbans,
        int? reservatDestiDespres,
        string? motiu)
    {
        return new StockMoviments
        {
            Tipus = tipus,
            IdProducte = idProducte,
            IdLot = idLot,
            IdUbicacioOrigen = idUbicacioOrigen,
            IdUbicacioDesti = idUbicacioDesti,
            Quantitat = quantitat,
            TotalOrigenAbans = totalOrigenAbans,
            TotalOrigenDespres = totalOrigenDespres,
            ReservatOrigenAbans = reservatOrigenAbans,
            ReservatOrigenDespres = reservatOrigenDespres,
            TotalDestiAbans = totalDestiAbans,
            TotalDestiDespres = totalDestiDespres,
            ReservatDestiAbans = reservatDestiAbans,
            ReservatDestiDespres = reservatDestiDespres,
            Motiu = string.IsNullOrWhiteSpace(motiu) ? null : motiu.Trim(),
            DataMoviment = DateTime.Now
        };
    }
}
