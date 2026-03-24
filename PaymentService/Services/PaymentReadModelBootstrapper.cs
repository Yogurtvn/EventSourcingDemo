namespace PaymentService.Services;

public sealed class PaymentReadModelBootstrapper(IServiceScopeFactory scopeFactory, ILogger<PaymentReadModelBootstrapper> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<PaymentEventService>();
        await service.RebuildReadModelAsync(cancellationToken);
        logger.LogInformation("Payment read model rebuilt from EventStore");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
