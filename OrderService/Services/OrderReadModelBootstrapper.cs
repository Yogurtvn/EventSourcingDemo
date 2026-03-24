namespace OrderService.Services;

public sealed class OrderReadModelBootstrapper(IServiceScopeFactory scopeFactory, ILogger<OrderReadModelBootstrapper> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var orderEventService = scope.ServiceProvider.GetRequiredService<OrderEventService>();

        await orderEventService.RebuildReadModelAsync(cancellationToken);
        logger.LogInformation("Order read model rebuilt from EventStore");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
