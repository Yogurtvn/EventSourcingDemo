using Shared.Contracts.Events;
using Shared.Messaging.RabbitMq;

namespace PaymentService.Services;

public sealed class PaymentSagaSubscriber(
    IRabbitMqEventBus eventBus,
    IServiceScopeFactory scopeFactory,
    ILogger<PaymentSagaSubscriber> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        eventBus.Subscribe<InventoryReserved>("payment.inventory-reserved", async @event =>
        {
            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<PaymentEventService>();
            await service.HandleInventoryReservedAsync(@event);
        });

        logger.LogInformation("Payment saga subscribers started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
