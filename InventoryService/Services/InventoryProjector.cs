using InventoryService.Infrastructure.Persistence;
using InventoryService.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Events;

namespace InventoryService.Services;

public sealed class InventoryProjector(InventoryDbContext dbContext)
{
    public async Task ApplyAsync(Event @event, CancellationToken cancellationToken = default)
    {
        switch (@event)
        {
            case InventoryReserved reserved:
                await UpsertAsync(reserved.AggregateId, "Reserved", null, reserved.Timestamp, reserved.Timestamp, null, cancellationToken);
                break;
            case InventoryFailed failed:
                await UpsertAsync(failed.AggregateId, "Failed", failed.Reason, failed.Timestamp, null, null, cancellationToken);
                break;
            case InventoryReleased released:
                await UpsertAsync(released.AggregateId, "Released", released.Reason, released.Timestamp, null, released.Timestamp, cancellationToken);
                break;
        }
    }

    private async Task UpsertAsync(
        Guid orderId,
        string status,
        string? reason,
        DateTime updatedAt,
        DateTime? reservedAt,
        DateTime? releasedAt,
        CancellationToken cancellationToken)
    {
        var model = await dbContext.Reservations.FirstOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);
        if (model is null)
        {
            model = new InventoryReservationReadModel
            {
                OrderId = orderId,
                ReservationStatus = status,
                Reason = reason,
                ReservedAt = reservedAt,
                ReleasedAt = releasedAt,
                LastUpdatedAt = updatedAt
            };
            dbContext.Reservations.Add(model);
        }
        else
        {
            model.ReservationStatus = status;
            model.Reason = reason;
            model.LastUpdatedAt = updatedAt;
            model.ReservedAt ??= reservedAt;
            model.ReleasedAt = releasedAt ?? model.ReleasedAt;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
