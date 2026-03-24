using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Infrastructure.Persistence;

namespace OrderService.Controllers;

[ApiController]
[Route("orders")]
[Route("api/orders")]
public sealed class OrderEventStoreController(OrderDbContext dbContext) : ControllerBase
{
    [HttpGet("events/recent")]
    public async Task<IActionResult> GetRecentEvents([FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        var safeTake = Math.Clamp(take, 1, 500);
        var events = await dbContext.EventStore
            .OrderByDescending(x => x.Timestamp)
            .ThenByDescending(x => x.Id)
            .Take(safeTake)
            .Select(x => new
            {
                x.Id,
                x.AggregateId,
                x.EventType,
                x.Timestamp,
                x.Version,
                x.EventData
            })
            .ToListAsync(cancellationToken);

        return Ok(events);
    }

    [HttpGet("{orderId:guid}/events")]
    [HttpGet("{orderId:guid}/event")]
    public async Task<IActionResult> GetOrderEvents(Guid orderId, CancellationToken cancellationToken)
    {
        var events = await dbContext.EventStore
            .Where(x => x.AggregateId == orderId)
            .OrderBy(x => x.Version)
            .Select(x => new
            {
                x.Id,
                x.AggregateId,
                x.EventType,
                x.Timestamp,
                x.Version,
                x.EventData
            })
            .ToListAsync(cancellationToken);

        return Ok(events);
    }
}
