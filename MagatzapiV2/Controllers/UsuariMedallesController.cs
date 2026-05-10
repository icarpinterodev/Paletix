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
public class UsuariMedallesController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsuariMedallesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<UsuariMedallesReadDto>>> GetUsuariMedalles([FromQuery] PaginationQuery query)
    {
        var source = _context.UsuariMedalles.AsNoTracking().OrderBy(entity => entity.Registre);
        var totalItems = await source.CountAsync();
        var entities = await source
            .Skip(query.Skip)
            .Take(query.PageSize)
            .ToListAsync();
        var items = entities.Select(EntityMappings.ToReadDto).ToList();

        return Ok(PagedResult<UsuariMedallesReadDto>.Create(items, query, totalItems));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UsuariMedallesReadDto>> GetUsuariMedalles(int id)
    {
        var entity = await _context.UsuariMedalles
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Registre == id);

        if (entity == null)
        {
            return NotFound();
        }

        return Ok(EntityMappings.ToReadDto(entity));
    }

    [HttpPost]
    public async Task<ActionResult<UsuariMedallesReadDto>> PostUsuariMedalles(UsuariMedallesRequestDto dto)
    {
        var entity = EntityMappings.ToEntity(dto);
        _context.UsuariMedalles.Add(entity);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            return this.DatabaseConflict(exception);
        }

        return CreatedAtAction(nameof(GetUsuariMedalles), new { id = entity.Registre }, EntityMappings.ToReadDto(entity));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutUsuariMedalles(int id, UsuariMedallesRequestDto dto)
    {
        var entity = await _context.UsuariMedalles.FirstOrDefaultAsync(entity => entity.Registre == id);
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
    public async Task<IActionResult> DeleteUsuariMedalles(int id)
    {
        var entity = await _context.UsuariMedalles.FirstOrDefaultAsync(entity => entity.Registre == id);
        if (entity == null)
        {
            return NotFound();
        }

        _context.UsuariMedalles.Remove(entity);

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
