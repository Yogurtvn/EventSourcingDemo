using Shared.Contracts.Events;
using Shared.Messaging.RabbitMq;

namespace OrderService.Services;

public sealed class OrderSagaSubscriber(
    IRabbitMqEventBus eventBus,
    IServiceScopeFactory scopeFactory,
    ILogger<OrderSagaSubscriber> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        eventBus.Subscribe<PaymentSucceeded>("order.payment-succeeded", async @event =>
        {
            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<OrderEventService>();
            await service.HandlePaymentSucceededAsync(@event);
        });

        eventBus.Subscribe<PaymentFailed>("order.payment-failed", async @event =>
        {
            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<OrderEventService>();
            await service.HandlePaymentFailedAsync(@event);
        });

        eventBus.Subscribe<InventoryFailed>("order.inventory-failed", async @event =>
        {
            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<OrderEventService>();
            await service.HandleInventoryFailedAsync(@event);
        });

        logger.LogInformation("Order saga subscribers started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
