using Shared.Contracts.Events;
using Shared.Messaging.RabbitMq;

namespace InventoryService.Services;

public sealed class InventorySagaSubscriber(
    IRabbitMqEventBus eventBus,
    IServiceScopeFactory scopeFactory,
    ILogger<InventorySagaSubscriber> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        eventBus.Subscribe<OrderCreated>("inventory.order-created", async @event =>
        {
            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<InventoryEventService>();
            await service.HandleOrderCreatedAsync(@event);
        });

        eventBus.Subscribe<PaymentFailed>("inventory.payment-failed", async @event =>
        {
            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<InventoryEventService>();
            await service.HandleRollbackAsync(@event.AggregateId, "PaymentFailed");
        });

        eventBus.Subscribe<OrderCancelled>("inventory.order-cancelled", async @event =>
        {
            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<InventoryEventService>();
            await service.HandleRollbackAsync(@event.AggregateId, @event.Reason);
        });

        logger.LogInformation("Inventory saga subscribers started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
