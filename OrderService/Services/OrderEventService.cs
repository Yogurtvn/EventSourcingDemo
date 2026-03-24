using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrderService.Contracts;
using OrderService.Infrastructure.Persistence;
using OrderService.Models;
using Shared.Contracts.Events;
using Shared.Messaging.RabbitMq;

namespace OrderService.Services;

public sealed class OrderEventService(
    OrderDbContext dbContext,
    OrderProjector projector,
    IRabbitMqEventBus eventBus,
    ILogger<OrderEventService> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Guid> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var orderId = Guid.NewGuid();
        var orderCreated = new OrderCreated(orderId, request.CustomerId, request.TotalAmount, request.TestScenario);

        await AppendEventAsync(orderCreated, cancellationToken);
        await eventBus.PublishAsync(orderCreated, cancellationToken);

        logger.LogInformation("OrderCreated published for OrderId {OrderId}", orderId);
        return orderId;
    }

    public async Task HandlePaymentSucceededAsync(PaymentSucceeded @event, CancellationToken cancellationToken = default)
    {
        await AppendEventAsync(@event, cancellationToken);

        var completed = new OrderCompleted(@event.AggregateId, "Payment successful");
        await AppendEventAsync(completed, cancellationToken);
        await eventBus.PublishAsync(completed, cancellationToken);

        logger.LogInformation("OrderCompleted published for OrderId {OrderId}", @event.AggregateId);
    }

    public async Task HandlePaymentFailedAsync(PaymentFailed @event, CancellationToken cancellationToken = default)
    {
        await AppendEventAsync(@event, cancellationToken);

        var cancelled = new OrderCancelled(@event.AggregateId, @event.Reason);
        await AppendEventAsync(cancelled, cancellationToken);
        await eventBus.PublishAsync(cancelled, cancellationToken);

        logger.LogInformation("OrderCancelled published for OrderId {OrderId}", @event.AggregateId);
    }

    public async Task HandleInventoryFailedAsync(InventoryFailed @event, CancellationToken cancellationToken = default)
    {
        await AppendEventAsync(@event, cancellationToken);

        var cancelled = new OrderCancelled(@event.AggregateId, @event.Reason);
        await AppendEventAsync(cancelled, cancellationToken);
        await eventBus.PublishAsync(cancelled, cancellationToken);

        logger.LogInformation("OrderCancelled (from InventoryFailed) published for OrderId {OrderId}", @event.AggregateId);
    }

    public async Task RebuildReadModelAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Orders.ExecuteDeleteAsync(cancellationToken);

        var stream = await dbContext.EventStore
            .OrderBy(x => x.Timestamp)
            .ThenBy(x => x.Version)
            .ToListAsync(cancellationToken);

        foreach (var entry in stream)
        {
            var @event = DeserializeEvent(entry);
            if (@event is null)
            {
                continue;
            }

            await projector.ApplyAsync(@event, cancellationToken);
        }
    }

    private async Task AppendEventAsync(Event @event, CancellationToken cancellationToken)
    {
        var currentVersion = await dbContext.EventStore
            .Where(x => x.AggregateId == @event.AggregateId)
            .CountAsync(cancellationToken);

        var entry = new EventStoreEntry
        {
            AggregateId = @event.AggregateId,
            EventType = @event.GetType().Name,
            EventData = JsonSerializer.Serialize(@event, @event.GetType(), JsonOptions),
            Timestamp = @event.Timestamp,
            Version = currentVersion + 1
        };

        dbContext.EventStore.Add(entry);
        await dbContext.SaveChangesAsync(cancellationToken);

        await projector.ApplyAsync(@event, cancellationToken);
    }

    private static Event? DeserializeEvent(EventStoreEntry entry)
    {
        var eventType = EventTypeMap.Resolve(entry.EventType);
        if (eventType is null)
        {
            return null;
        }

        return JsonSerializer.Deserialize(entry.EventData, eventType, JsonOptions) as Event;
    }
}
