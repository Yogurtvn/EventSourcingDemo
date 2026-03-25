using Shared.Contracts.Events;
using Shared.Messaging.RabbitMq;

namespace NotificationService.Services;

public sealed class NotificationSagaSubscriber(
    IRabbitMqEventBus eventBus,
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<NotificationSagaSubscriber> logger) : IHostedService
{
    private readonly string _orderServiceBaseUrl =
        (configuration["OrderService:BaseUrl"] ?? "http://localhost:5044").TrimEnd('/');

    public Task StartAsync(CancellationToken cancellationToken)
    {
        eventBus.Subscribe<OrderCreated>("notification.order-created", async @event =>
        {
            using var scope = scopeFactory.CreateScope();
            var ledger = scope.ServiceProvider.GetRequiredService<NotificationLedgerService>();
            await ledger.HandleOrderCreatedAsync(@event);
        });

        eventBus.Subscribe<OrderCompleted>("notification.order-completed", async @event =>
        {
            using var scope = scopeFactory.CreateScope();
            var ledger = scope.ServiceProvider.GetRequiredService<NotificationLedgerService>();
            await ledger.HandleOrderCompletedAsync(@event);
        });

        eventBus.Subscribe<OrderCancelled>("notification.order-cancelled", async @event =>
        {
            using var scope = scopeFactory.CreateScope();
            var ledger = scope.ServiceProvider.GetRequiredService<NotificationLedgerService>();
            await ledger.HandleOrderCancelledAsync(@event);
        });

        logger.LogInformation("Notification saga subscribers started (OrderService at {BaseUrl})", _orderServiceBaseUrl);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
