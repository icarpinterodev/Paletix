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
public class UbicacionsController : ControllerBase
{
    private readonly AppDbContext _context;

    public UbicacionsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<UbicacionsReadDto>>> GetUbicacions([FromQuery] PaginationQuery query)
    {
        var source = _context.Ubicacions.AsNoTracking().OrderBy(entity => entity.Id);
        var totalItems = await source.CountAsync();
        var entities = await source
            .Skip(query.Skip)
            .Take(query.PageSize)
            .ToListAsync();
        var items = entities.Select(EntityMappings.ToReadDto).ToList();

        return Ok(PagedResult<UbicacionsReadDto>.Create(items, query, totalItems));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UbicacionsReadDto>> GetUbicacions(int id)
    {
        var entity = await _context.Ubicacions
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == id);

        if (entity == null)
        {
            return NotFound();
        }

        return Ok(EntityMappings.ToReadDto(entity));
    }

    [HttpPost]
    public async Task<ActionResult<UbicacionsReadDto>> PostUbicacions(UbicacionsRequestDto dto)
    {
        var entity = EntityMappings.ToEntity(dto);
        _context.Ubicacions.Add(entity);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            return this.DatabaseConflict(exception);
        }

        return CreatedAtAction(nameof(GetUbicacions), new { id = entity.Id }, EntityMappings.ToReadDto(entity));
    }

    [HttpPost("bulk")]
    public async Task<ActionResult<IReadOnlyList<UbicacionsReadDto>>> PostUbicacionsBulk(IReadOnlyList<UbicacionsRequestDto> dtos)
    {
        if (dtos.Count == 0)
        {
            return BadRequest("Cal enviar com a minim una ubicacio.");
        }

        if (dtos.Count > 1000)
        {
            return BadRequest("No es poden crear mes de 1000 ubicacions per operacio.");
        }

        var duplicateInRequest = dtos
            .GroupBy(dto => new { dto.Zona, dto.Passadis, dto.BlocEstanteria, dto.Fila, dto.Columna })
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateInRequest is not null)
        {
            return Conflict("La peticio conte ubicacions duplicades.");
        }

        var entities = dtos.Select(EntityMappings.ToEntity).ToList();
        _context.Ubicacions.AddRange(entities);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            return this.DatabaseConflict(exception);
        }

        return Ok(entities.Select(EntityMappings.ToReadDto).ToList());
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutUbicacions(int id, UbicacionsRequestDto dto)
    {
        var entity = await _context.Ubicacions.FirstOrDefaultAsync(entity => entity.Id == id);
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
    public async Task<IActionResult> DeleteUbicacions(int id)
    {
        var entity = await _context.Ubicacions.FirstOrDefaultAsync(entity => entity.Id == id);
        if (entity == null)
        {
            return NotFound();
        }

        _context.Ubicacions.Remove(entity);

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
}
