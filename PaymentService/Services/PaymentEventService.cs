using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PaymentService.Infrastructure.Persistence;
using PaymentService.Models;
using Shared.Contracts.Events;
using Shared.Messaging.RabbitMq;

namespace PaymentService.Services;

public sealed class PaymentEventService(
    PaymentDbContext dbContext,
    PaymentProjector projector,
    IRabbitMqEventBus eventBus,
    ILogger<PaymentEventService> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task HandleInventoryReservedAsync(InventoryReserved @event, CancellationToken cancellationToken = default)
    {
        var scenario = @event.TestScenario?.Trim().ToLowerInvariant();
        var success = scenario switch
        {
            "payment-fail" => false,
            "happy" => true,
            _ => Random.Shared.Next(0, 100) >= 50
        };

        if (success)
        {
            var succeeded = new PaymentSucceeded(@event.AggregateId, Guid.NewGuid(), Random.Shared.Next(20, 500));
            await AppendEventAsync(succeeded, cancellationToken);
            await eventBus.PublishAsync(succeeded, cancellationToken);
            logger.LogInformation("PaymentSucceeded published for OrderId {OrderId}", @event.AggregateId);
            return;
        }

        var failed = new PaymentFailed(@event.AggregateId, "PaymentGatewayTimeout");
        await AppendEventAsync(failed, cancellationToken);
        await eventBus.PublishAsync(failed, cancellationToken);
        logger.LogInformation("PaymentFailed published for OrderId {OrderId}", @event.AggregateId);
    }

    public async Task RebuildReadModelAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Payments.ExecuteDeleteAsync(cancellationToken);

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
