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
public class PagamentsController : ControllerBase
{
    private readonly AppDbContext _context;

    public PagamentsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<PagamentsReadDto>>> GetPagaments([FromQuery] PaginationQuery query)
    {
        var source = _context.Pagaments.AsNoTracking().OrderBy(entity => entity.Id);
        var totalItems = await source.CountAsync();
        var entities = await source
            .Skip(query.Skip)
            .Take(query.PageSize)
            .ToListAsync();
        var items = entities.Select(EntityMappings.ToReadDto).ToList();

        return Ok(PagedResult<PagamentsReadDto>.Create(items, query, totalItems));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PagamentsReadDto>> GetPagaments(int id)
    {
        var entity = await _context.Pagaments
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == id);

        if (entity == null)
        {
            return NotFound();
        }

        return Ok(EntityMappings.ToReadDto(entity));
    }

    [HttpPost]
    public async Task<ActionResult<PagamentsReadDto>> PostPagaments(PagamentsRequestDto dto)
    {
        var entity = EntityMappings.ToEntity(dto);
        _context.Pagaments.Add(entity);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            return this.DatabaseConflict(exception);
        }

        return CreatedAtAction(nameof(GetPagaments), new { id = entity.Id }, EntityMappings.ToReadDto(entity));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutPagaments(int id, PagamentsRequestDto dto)
    {
        var entity = await _context.Pagaments.FirstOrDefaultAsync(entity => entity.Id == id);
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
    public async Task<IActionResult> DeletePagaments(int id)
    {
        var entity = await _context.Pagaments.FirstOrDefaultAsync(entity => entity.Id == id);
        if (entity == null)
        {
            return NotFound();
        }

        _context.Pagaments.Remove(entity);

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
