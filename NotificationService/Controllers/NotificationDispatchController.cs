using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotificationService.Infrastructure.Persistence;

namespace NotificationService.Controllers;

[ApiController]
[Route("api/notifications")]
[Route("notifications")]
public sealed class NotificationDispatchController(NotificationDbContext dbContext) : ControllerBase
{
    /// <summary>
    /// Lịch sử gửi thông báo (audit + idempotency log).
    /// </summary>
    [HttpGet("dispatches")]
    public async Task<IActionResult> GetDispatches([FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        var safeTake = Math.Clamp(take, 1, 500);
        var items = await dbContext.NotificationSendLogs
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(safeTake)
            .Select(x => new
            {
                x.Id,
                x.IdempotencyKey,
                x.EventType,
                x.AggregateId,
                x.RecipientEmail,
                x.Subject,
                x.Status,
                x.ErrorDetail,
                x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(items);
    }
}
