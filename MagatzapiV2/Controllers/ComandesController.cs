using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MagatzapiV2.Data;
using MagatzapiV2.Dtos;
using SharedContracts.Dtos;
using MagatzapiV2.Infrastructure;
using MagatzapiV2.Models;

namespace MagatzapiV2.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class ComandesController : ControllerBase
{
    private readonly AppDbContext _context;

    public ComandesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ComandesReadDto>>> GetComandes([FromQuery] PaginationQuery query)
    {
        var source = _context.Comandes
            .AsNoTracking()
            .Include(entity => entity.IdEstatNavigation)
            .Include(entity => entity.Liniescomanda)
                .ThenInclude(linia => linia.IdUbicacioNavigation)
            .OrderBy(entity => entity.Id);
        var totalItems = await source.CountAsync();
        var entities = await source
            .Skip(query.Skip)
            .Take(query.PageSize)
            .ToListAsync();
        var items = entities.Select(ToComandaReadDto).ToList();

        return Ok(PagedResult<ComandesReadDto>.Create(items, query, totalItems));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ComandesReadDto>> GetComandes(int id)
    {
        var entity = await _context.Comandes
            .AsNoTracking()
            .Include(entity => entity.IdEstatNavigation)
            .Include(entity => entity.Liniescomanda)
                .ThenInclude(linia => linia.IdUbicacioNavigation)
            .FirstOrDefaultAsync(entity => entity.Id == id);

        if (entity == null)
        {
            return NotFound();
        }

        return Ok(ToComandaReadDto(entity));
    }

    [HttpPost]
    public async Task<ActionResult<ComandesReadDto>> PostComandes(ComandesRequestDto dto)
    {
        var estatId = await ResolveEstatIdAsync(dto.IdEstat, dto.Estat);
        if (estatId == null)
        {
            return BadRequest("Cal indicar un estat valid amb idEstat o estat.");
        }

        var liniesResult = await BuildLiniesAsync(dto.Linies);
        if (liniesResult.Error != null)
        {
            return BadRequest(liniesResult.Error);
        }

        var entity = EntityMappings.ToEntity(dto);
        entity.IdEstat = estatId.Value;
        foreach (var linia in liniesResult.Linies)
        {
            entity.Liniescomanda.Add(linia);
        }

        _context.Comandes.Add(entity);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            return this.DatabaseConflict(exception);
        }

        var created = await LoadComandaAsync(entity.Id);
        return CreatedAtAction(nameof(GetComandes), new { id = entity.Id }, ToComandaReadDto(created!));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutComandes(int id, ComandesRequestDto dto)
    {
        var entity = await _context.Comandes
            .Include(entity => entity.Liniescomanda)
            .FirstOrDefaultAsync(entity => entity.Id == id);
        if (entity == null)
        {
            return NotFound();
        }

        var estatId = await ResolveEstatIdAsync(dto.IdEstat, dto.Estat);
        if (estatId == null)
        {
            return BadRequest("Cal indicar un estat valid amb idEstat o estat.");
        }

        var liniesResult = await BuildLiniesAsync(dto.Linies);
        if (liniesResult.Error != null)
        {
            return BadRequest(liniesResult.Error);
        }

        EntityMappings.ApplyUpdate(dto, entity);
        entity.IdEstat = estatId.Value;

        _context.Liniescomanda.RemoveRange(entity.Liniescomanda);
        foreach (var linia in liniesResult.Linies)
        {
            linia.IdComanda = id;
            _context.Liniescomanda.Add(linia);
        }

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
    public async Task<IActionResult> DeleteComandes(int id)
    {
        var entity = await _context.Comandes
            .Include(entity => entity.Liniescomanda)
            .FirstOrDefaultAsync(entity => entity.Id == id);
        if (entity == null)
        {
            return NotFound();
        }

        _context.Liniescomanda.RemoveRange(entity.Liniescomanda);
        _context.Comandes.Remove(entity);

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

    private async Task<Comandes?> LoadComandaAsync(int id)
    {
        return await _context.Comandes
            .AsNoTracking()
            .Include(entity => entity.IdEstatNavigation)
            .Include(entity => entity.Liniescomanda)
                .ThenInclude(linia => linia.IdUbicacioNavigation)
            .FirstOrDefaultAsync(entity => entity.Id == id);
    }

    private static ComandesReadDto ToComandaReadDto(Comandes entity)
    {
        var dto = EntityMappings.ToReadDto(entity);
        dto.Estat = entity.IdEstatNavigation.Codi;
        dto.Linies = entity.Liniescomanda
            .OrderBy(linia => linia.Id)
            .Select(linia => new ComandesLiniaReadDto
            {
                Id = linia.Id,
                IdProducte = linia.IdProducte,
                IdUbicacio = linia.IdUbicacio,
                Ubicacio = linia.IdUbicacioNavigation.CodiGenerat,
                Palets = linia.Palets,
                Caixes = linia.Caixes,
                IdEstatVerificacio = linia.IdEstatVerificacio
            })
            .ToList();

        return dto;
    }

    private async Task<int?> ResolveEstatIdAsync(int idEstat, string? estat)
    {
        if (!string.IsNullOrWhiteSpace(estat))
        {
            return await _context.Estats
                .Where(entity => entity.Codi == estat || entity.Descripcio == estat)
                .Select(entity => (int?)entity.Id)
                .FirstOrDefaultAsync();
        }

        if (idEstat <= 0)
        {
            return null;
        }

        return await _context.Estats
            .Where(entity => entity.Id == idEstat)
            .Select(entity => (int?)entity.Id)
            .FirstOrDefaultAsync();
    }

    private async Task<(List<Liniescomanda> Linies, string? Error)> BuildLiniesAsync(IEnumerable<ComandesLiniaRequestDto>? dtos)
    {
        var linies = new List<Liniescomanda>();
        if (dtos == null)
        {
            return (linies, null);
        }

        var index = 0;

        foreach (var dto in dtos)
        {
            index++;
            if (dto.IdProducte <= 0)
            {
                return (linies, $"La linia {index} ha d'indicar un idProducte valid.");
            }

            if (dto.Caixes <= 0)
            {
                return (linies, $"La linia {index} ha d'indicar un nombre de caixes superior a 0.");
            }

            var ubicacioId = await ResolveUbicacioIdAsync(dto);
            if (ubicacioId == null)
            {
                return (linies, $"La linia {index} ha d'indicar una ubicacio valida amb idUbicacio o ubicacio.");
            }

            linies.Add(new Liniescomanda
            {
                IdProducte = dto.IdProducte,
                IdUbicacio = ubicacioId.Value,
                Palets = dto.Palets,
                Caixes = dto.Caixes,
                IdEstatVerificacio = dto.IdEstatVerificacio
            });
        }

        return (linies, null);
    }

    private async Task<int?> ResolveUbicacioIdAsync(ComandesLiniaRequestDto dto)
    {
        if (dto.IdUbicacio.HasValue && dto.IdUbicacio.Value > 0)
        {
            return await _context.Ubicacions
                .Where(entity => entity.Id == dto.IdUbicacio.Value)
                .Select(entity => (int?)entity.Id)
                .FirstOrDefaultAsync();
        }

        if (string.IsNullOrWhiteSpace(dto.Ubicacio))
        {
            return null;
        }

        return await _context.Ubicacions
            .Where(entity => entity.CodiGenerat == dto.Ubicacio)
            .Select(entity => (int?)entity.Id)
            .FirstOrDefaultAsync();
    }
}
