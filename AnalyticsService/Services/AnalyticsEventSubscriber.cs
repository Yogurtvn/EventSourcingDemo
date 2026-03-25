using Shared.Contracts.Events;
using Shared.Messaging.RabbitMq;

namespace AnalyticsService.Services;

public sealed class AnalyticsEventSubscriber(
    IRabbitMqEventBus eventBus,
    IServiceScopeFactory scopeFactory,
    ILogger<AnalyticsEventSubscriber> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        eventBus.Subscribe<OrderCreated>("analytics.order-created", async @event =>
        {
            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<AnalyticsEventService>();
            await service.HandleOrderCreatedAsync(@event, cancellationToken);
        });

        eventBus.Subscribe<OrderCompleted>("analytics.order-completed", async @event =>
        {
            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<AnalyticsEventService>();
            await service.HandleOrderCompletedAsync(@event, cancellationToken);
        });

        eventBus.Subscribe<OrderCancelled>("analytics.order-cancelled", async @event =>
        {
            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<AnalyticsEventService>();
            await service.HandleOrderCancelledAsync(@event, cancellationToken);
        });

        logger.LogInformation("Analytics event subscribers started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Analytics event subscribers stopped");
        return Task.CompletedTask;
    }
}
