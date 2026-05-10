using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MagatzapiV2.Data;
using MagatzapiV2.Dtos;
using SharedContracts.Dtos;

namespace MagatzapiV2.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class StockMovimentsController : ControllerBase
{
    private readonly AppDbContext _context;

    public StockMovimentsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<StockMovimentReadDto>>> GetStockMoviments([FromQuery] PaginationQuery query)
    {
        var source = _context.StockMoviments.AsNoTracking().OrderByDescending(entity => entity.DataMoviment).ThenByDescending(entity => entity.Id);
        var totalItems = await source.CountAsync();
        var entities = await source
            .Skip(query.Skip)
            .Take(query.PageSize)
            .ToListAsync();
        var items = entities.Select(EntityMappings.ToReadDto).ToList();

        return Ok(PagedResult<StockMovimentReadDto>.Create(items, query, totalItems));
    }
}
