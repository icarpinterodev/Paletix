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
public class TipusVehiclesController : ControllerBase
{
    private readonly AppDbContext _context;

    public TipusVehiclesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<TipusVehiclesReadDto>>> GetTipusVehicles([FromQuery] PaginationQuery query)
    {
        var source = _context.TipusVehicles.AsNoTracking().OrderBy(entity => entity.Id);
        var totalItems = await source.CountAsync();
        var entities = await source
            .Skip(query.Skip)
            .Take(query.PageSize)
            .ToListAsync();
        var items = entities.Select(EntityMappings.ToReadDto).ToList();

        return Ok(PagedResult<TipusVehiclesReadDto>.Create(items, query, totalItems));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TipusVehiclesReadDto>> GetTipusVehicles(int id)
    {
        var entity = await _context.TipusVehicles
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == id);

        if (entity == null)
        {
            return NotFound();
        }

        return Ok(EntityMappings.ToReadDto(entity));
    }

    [HttpPost]
    public async Task<ActionResult<TipusVehiclesReadDto>> PostTipusVehicles(TipusVehiclesRequestDto dto)
    {
        var entity = EntityMappings.ToEntity(dto);
        _context.TipusVehicles.Add(entity);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            return this.DatabaseConflict(exception);
        }

        return CreatedAtAction(nameof(GetTipusVehicles), new { id = entity.Id }, EntityMappings.ToReadDto(entity));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutTipusVehicles(int id, TipusVehiclesRequestDto dto)
    {
        var entity = await _context.TipusVehicles.FirstOrDefaultAsync(entity => entity.Id == id);
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
    public async Task<IActionResult> DeleteTipusVehicles(int id)
    {
        var entity = await _context.TipusVehicles.FirstOrDefaultAsync(entity => entity.Id == id);
        if (entity == null)
        {
            return NotFound();
        }

        _context.TipusVehicles.Remove(entity);

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
