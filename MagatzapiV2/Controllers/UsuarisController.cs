using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MagatzapiV2.Data;
using MagatzapiV2.Dtos;
using SharedContracts.Dtos;
using MagatzapiV2.Infrastructure;
using MagatzapiV2.Models;
using Microsoft.AspNetCore.Identity;

namespace MagatzapiV2.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class UsuarisController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher<Usuaris> _passwordHasher = new();

    public UsuarisController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<UsuarisReadDto>>> GetUsuaris([FromQuery] PaginationQuery query)
    {
        var source = _context.Usuaris.AsNoTracking().OrderBy(entity => entity.Id);
        var totalItems = await source.CountAsync();
        var entities = await source
            .Skip(query.Skip)
            .Take(query.PageSize)
            .ToListAsync();
        var items = entities.Select(EntityMappings.ToReadDto).ToList();

        return Ok(PagedResult<UsuarisReadDto>.Create(items, query, totalItems));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UsuarisReadDto>> GetUsuaris(int id)
    {
        var entity = await _context.Usuaris
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == id);

        if (entity == null)
        {
            return NotFound();
        }

        return Ok(EntityMappings.ToReadDto(entity));
    }

    [HttpPost]
    public async Task<ActionResult<UsuarisReadDto>> PostUsuaris(UsuarisRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 8)
        {
            return BadRequest("La contrasenya ha de tenir com a minim 8 caracters.");
        }

        var entity = EntityMappings.ToEntity(dto);
        entity.Password = _passwordHasher.HashPassword(entity, dto.Password);
        _context.Usuaris.Add(entity);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            return this.DatabaseConflict(exception);
        }

        return CreatedAtAction(nameof(GetUsuaris), new { id = entity.Id }, EntityMappings.ToReadDto(entity));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutUsuaris(int id, UsuarisRequestDto dto)
    {
        var entity = await _context.Usuaris.FirstOrDefaultAsync(entity => entity.Id == id);
        if (entity == null)
        {
            return NotFound();
        }

        var previousPassword = entity.Password;
        EntityMappings.ApplyUpdate(dto, entity);
        if (string.IsNullOrWhiteSpace(dto.Password))
        {
            entity.Password = previousPassword;
        }
        else if (dto.Password.Length < 8)
        {
            return BadRequest("La contrasenya ha de tenir com a minim 8 caracters.");
        }
        else
        {
            entity.Password = _passwordHasher.HashPassword(entity, dto.Password);
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
    public async Task<IActionResult> DeleteUsuaris(int id)
    {
        var entity = await _context.Usuaris.FirstOrDefaultAsync(entity => entity.Id == id);
        if (entity == null)
        {
            return NotFound();
        }

        _context.Usuaris.Remove(entity);

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
