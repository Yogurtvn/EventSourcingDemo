using System.Text.Json;
using InventoryService.Infrastructure.Persistence;
using InventoryService.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Events;
using Shared.Messaging.RabbitMq;

namespace InventoryService.Services;

public sealed class InventoryEventService(
    InventoryDbContext dbContext,
    InventoryProjector projector,
    IRabbitMqEventBus eventBus,
    ILogger<InventoryEventService> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task HandleOrderCreatedAsync(OrderCreated @event, CancellationToken cancellationToken = default)
    {
        var scenario = @event.TestScenario?.Trim().ToLowerInvariant();
        var canReserve = scenario switch
        {
            "inventory-fail" => false,
            "happy" => true,
            "payment-fail" => true,
            _ => Random.Shared.Next(0, 100) >= 20
        };

        if (canReserve)
        {
            var reserved = new InventoryReserved(@event.AggregateId, DateTime.UtcNow.AddMinutes(15), @event.TestScenario);
            await AppendEventAsync(reserved, cancellationToken);
            await eventBus.PublishAsync(reserved, cancellationToken);
            logger.LogInformation("InventoryReserved published for OrderId {OrderId}", @event.AggregateId);
            return;
        }

        var failed = new InventoryFailed(@event.AggregateId, "InventoryUnavailable");
        await AppendEventAsync(failed, cancellationToken);
        await eventBus.PublishAsync(failed, cancellationToken);
        logger.LogInformation("InventoryFailed published for OrderId {OrderId}", @event.AggregateId);
    }

    public async Task HandleRollbackAsync(Guid aggregateId, string reason, CancellationToken cancellationToken = default)
    {
        var reservation = await dbContext.Reservations.FirstOrDefaultAsync(x => x.OrderId == aggregateId, cancellationToken);
        if (reservation is null || reservation.ReservationStatus != "Reserved")
        {
            return;
        }

        var released = new InventoryReleased(aggregateId, reason);
        await AppendEventAsync(released, cancellationToken);
        await eventBus.PublishAsync(released, cancellationToken);

        logger.LogInformation("InventoryReleased published for OrderId {OrderId}", aggregateId);
    }

    public async Task RebuildReadModelAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Reservations.ExecuteDeleteAsync(cancellationToken);

        var stream = await dbContext.EventStore
            .OrderBy(x => x.Timestamp)
            .ThenBy(x => x.Version)
            .ToListAsync(cancellationToken);

        foreach (var entry in stream)
        {
            var eventType = EventTypeMap.Resolve(entry.EventType);
            if (eventType is null)
            {
                continue;
            }

            var @event = JsonSerializer.Deserialize(entry.EventData, eventType, JsonOptions) as Event;
            if (@event is not null)
            {
                await projector.ApplyAsync(@event, cancellationToken);
            }
        }
    }

    private async Task AppendEventAsync(Event @event, CancellationToken cancellationToken)
    {
        var version = await dbContext.EventStore
            .Where(x => x.AggregateId == @event.AggregateId)
            .CountAsync(cancellationToken);

        var entry = new EventStoreEntry
        {
            AggregateId = @event.AggregateId,
            EventType = @event.GetType().Name,
            EventData = JsonSerializer.Serialize(@event, @event.GetType(), JsonOptions),
            Timestamp = @event.Timestamp,
            Version = version + 1
        };

        dbContext.EventStore.Add(entry);
        await dbContext.SaveChangesAsync(cancellationToken);
        await projector.ApplyAsync(@event, cancellationToken);
    }
}
