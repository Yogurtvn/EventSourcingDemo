namespace InventoryService.Services;

public sealed class InventoryReadModelBootstrapper(IServiceScopeFactory scopeFactory, ILogger<InventoryReadModelBootstrapper> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<InventoryEventService>();
        await service.RebuildReadModelAsync(cancellationToken);
        logger.LogInformation("Inventory read model rebuilt from EventStore");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
