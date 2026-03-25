using NotificationService.Infrastructure.Http;
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
            var catalog = scope.ServiceProvider.GetRequiredService<OrderCatalogClient>();
            var emailService = scope.ServiceProvider.GetRequiredService<NotificationEmailService>();
            var templateService = scope.ServiceProvider.GetRequiredService<EmailTemplateService>();

            var email = await catalog.GetEmailByCustomerIdAsync(@event.CustomerId);
            if (string.IsNullOrWhiteSpace(email))
            {
                logger.LogWarning("No email for customer {CustomerId}; skip OrderCreated notification", @event.CustomerId);
                return;
            }

            var subject = "Xác nhận đơn hàng";
            var htmlBody = templateService.GetOrderCreatedHtml(@event.AggregateId, @event.CustomerId, @event.TotalAmount);
            await emailService.SendAsync(email, subject, htmlBody);
        });

        eventBus.Subscribe<OrderCompleted>("notification.order-completed", async @event =>
        {
            using var scope = scopeFactory.CreateScope();
            var catalog = scope.ServiceProvider.GetRequiredService<OrderCatalogClient>();
            var emailService = scope.ServiceProvider.GetRequiredService<NotificationEmailService>();
            var templateService = scope.ServiceProvider.GetRequiredService<EmailTemplateService>();

            var email = await catalog.GetEmailByOrderIdAsync(@event.AggregateId);
            if (string.IsNullOrWhiteSpace(email))
            {
                logger.LogWarning("No email for order {OrderId}; skip OrderCompleted notification", @event.AggregateId);
                return;
            }

            var subject = "Đơn hàng hoàn tất";
            var htmlBody = templateService.GetOrderCompletedHtml(@event.AggregateId);
            await emailService.SendAsync(email, subject, htmlBody);
        });

        eventBus.Subscribe<OrderCancelled>("notification.order-cancelled", async @event =>
        {
            using var scope = scopeFactory.CreateScope();
            var catalog = scope.ServiceProvider.GetRequiredService<OrderCatalogClient>();
            var emailService = scope.ServiceProvider.GetRequiredService<NotificationEmailService>();
            var templateService = scope.ServiceProvider.GetRequiredService<EmailTemplateService>();

            var email = await catalog.GetEmailByOrderIdAsync(@event.AggregateId);
            if (string.IsNullOrWhiteSpace(email))
            {
                logger.LogWarning("No email for order {OrderId}; skip OrderCancelled notification", @event.AggregateId);
                return;
            }

            var subject = "Thông báo hủy đơn hàng";
            var htmlBody = templateService.GetOrderCancelledHtml(@event.AggregateId, @event.Reason);
            await emailService.SendAsync(email, subject, htmlBody);
        });

        logger.LogInformation("Notification saga subscribers started (OrderService at {BaseUrl})", _orderServiceBaseUrl);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
