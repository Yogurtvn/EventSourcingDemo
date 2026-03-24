using InventoryService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Controllers;

[ApiController]
[Route("api/inventory")]
public sealed class InventoryQueryController(InventoryDbContext dbContext) : ControllerBase
{
    [HttpGet("{orderId:guid}")]
    public async Task<IActionResult> GetByOrderId(Guid orderId, CancellationToken cancellationToken)
    {
        var model = await dbContext.Reservations.FirstOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);
        return model is null ? NotFound() : Ok(model);
    }
}
