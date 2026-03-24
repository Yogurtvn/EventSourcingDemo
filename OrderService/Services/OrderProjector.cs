using Microsoft.EntityFrameworkCore;
using OrderService.Infrastructure.Persistence;
using OrderService.Models;
using Shared.Contracts.Events;

namespace OrderService.Services;

public sealed class OrderProjector(OrderDbContext dbContext)
{
    public async Task ApplyAsync(Event @event, CancellationToken cancellationToken = default)
    {
        switch (@event)
        {
            case OrderCreated created:
                await ApplyOrderCreatedAsync(created, cancellationToken);
                break;
            case PaymentSucceeded succeeded:
                await UpdateStatusAsync(succeeded.AggregateId, "PaymentSucceeded", succeeded.Timestamp, cancellationToken);
                break;
            case PaymentFailed failed:
                await UpdateStatusAsync(failed.AggregateId, "PaymentFailed", failed.Timestamp, cancellationToken);
                break;
            case InventoryFailed inventoryFailed:
                await UpdateStatusAsync(inventoryFailed.AggregateId, "InventoryFailed", inventoryFailed.Timestamp, cancellationToken);
                break;
            case OrderCompleted completed:
                await UpdateStatusAsync(completed.AggregateId, "Completed", completed.Timestamp, cancellationToken);
                break;
            case OrderCancelled cancelled:
                await UpdateStatusAsync(cancelled.AggregateId, "Cancelled", cancelled.Timestamp, cancellationToken);
                break;
        }
    }

    private async Task ApplyOrderCreatedAsync(OrderCreated @event, CancellationToken cancellationToken)
    {
        var existing = await dbContext.Orders.FirstOrDefaultAsync(x => x.OrderId == @event.AggregateId, cancellationToken);
        if (existing is null)
        {
            dbContext.Orders.Add(new OrderReadModel
            {
                OrderId = @event.AggregateId,
                CustomerId = @event.CustomerId,
                TotalAmount = @event.TotalAmount,
                Status = "Created",
                LastUpdatedAt = @event.Timestamp
            });
        }
        else
        {
            existing.CustomerId = @event.CustomerId;
            existing.TotalAmount = @event.TotalAmount;
            existing.Status = "Created";
            existing.LastUpdatedAt = @event.Timestamp;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task UpdateStatusAsync(Guid orderId, string status, DateTime updatedAt, CancellationToken cancellationToken)
    {
        var readModel = await dbContext.Orders.FirstOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);
        if (readModel is null)
        {
            return;
        }

        readModel.Status = status;
        readModel.LastUpdatedAt = updatedAt;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
