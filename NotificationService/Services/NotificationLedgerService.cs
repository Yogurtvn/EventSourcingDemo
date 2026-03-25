using Microsoft.EntityFrameworkCore;
using NotificationService.Infrastructure.Http;
using NotificationService.Infrastructure.Persistence;
using NotificationService.Models;
using Shared.Contracts.Events;

namespace NotificationService.Services;

public sealed class NotificationLedgerService(
    NotificationDbContext db,
    OrderCatalogClient catalog,
    NotificationEmailService emailService,
    EmailTemplateService templates,
    ILogger<NotificationLedgerService> logger)
{
    public async Task HandleOrderCreatedAsync(OrderCreated e, CancellationToken cancellationToken = default)
    {
        var key = $"OrderCreated:{e.AggregateId:N}";
        if (await AlreadyHandledAsync(key, cancellationToken))
        {
            return;
        }

        const string subject = "Xác nhận đơn hàng";
        var html = templates.GetOrderCreatedHtml(e.AggregateId, e.CustomerId, e.TotalAmount);
        var email = await catalog.GetEmailByCustomerIdAsync(e.CustomerId, cancellationToken);
        if (string.IsNullOrWhiteSpace(email))
        {
            logger.LogWarning("No email for customer {CustomerId}; skip OrderCreated notification", e.CustomerId);
            await AddLogSafeAsync(
                CreateLog(key, nameof(OrderCreated), e.AggregateId, null, subject, NotificationDispatchStatus.SkippedNoRecipient, null),
                cancellationToken);
            return;
        }

        var outcome = await emailService.SendWithOutcomeAsync(email, subject, html, cancellationToken);
        var (status, err) = MapOutcome(outcome);
        await AddLogSafeAsync(
            CreateLog(key, nameof(OrderCreated), e.AggregateId, email, subject, status, err),
            cancellationToken);
    }

    public async Task HandleOrderCompletedAsync(OrderCompleted e, CancellationToken cancellationToken = default)
    {
        var key = $"OrderCompleted:{e.AggregateId:N}";
        if (await AlreadyHandledAsync(key, cancellationToken))
        {
            return;
        }

        const string subject = "Đơn hàng hoàn tất";
        var html = templates.GetOrderCompletedHtml(e.AggregateId);
        var email = await catalog.GetEmailByOrderIdAsync(e.AggregateId, cancellationToken);
        if (string.IsNullOrWhiteSpace(email))
        {
            logger.LogWarning("No email for order {OrderId}; skip OrderCompleted notification", e.AggregateId);
            await AddLogSafeAsync(
                CreateLog(key, nameof(OrderCompleted), e.AggregateId, null, subject, NotificationDispatchStatus.SkippedNoRecipient, null),
                cancellationToken);
            return;
        }

        var outcome = await emailService.SendWithOutcomeAsync(email, subject, html, cancellationToken);
        var (status, err) = MapOutcome(outcome);
        await AddLogSafeAsync(
            CreateLog(key, nameof(OrderCompleted), e.AggregateId, email, subject, status, err),
            cancellationToken);
    }

    public async Task HandleOrderCancelledAsync(OrderCancelled e, CancellationToken cancellationToken = default)
    {
        var key = $"OrderCancelled:{e.AggregateId:N}";
        if (await AlreadyHandledAsync(key, cancellationToken))
        {
            return;
        }

        const string subject = "Thông báo hủy đơn hàng";
        var html = templates.GetOrderCancelledHtml(e.AggregateId, e.Reason);
        var email = await catalog.GetEmailByOrderIdAsync(e.AggregateId, cancellationToken);
        if (string.IsNullOrWhiteSpace(email))
        {
            logger.LogWarning("No email for order {OrderId}; skip OrderCancelled notification", e.AggregateId);
            await AddLogSafeAsync(
                CreateLog(key, nameof(OrderCancelled), e.AggregateId, null, subject, NotificationDispatchStatus.SkippedNoRecipient, null),
                cancellationToken);
            return;
        }

        var outcome = await emailService.SendWithOutcomeAsync(email, subject, html, cancellationToken);
        var (status, err) = MapOutcome(outcome);
        await AddLogSafeAsync(
            CreateLog(key, nameof(OrderCancelled), e.AggregateId, email, subject, status, err),
            cancellationToken);
    }

    private async Task<bool> AlreadyHandledAsync(string idempotencyKey, CancellationToken cancellationToken)
    {
        var exists = await db.NotificationSendLogs.AsNoTracking()
            .AnyAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
        if (exists)
        {
            logger.LogInformation("Idempotent skip notification {Key}", idempotencyKey);
        }

        return exists;
    }

    private static NotificationSendLog CreateLog(
        string idempotencyKey,
        string eventType,
        Guid aggregateId,
        string? recipientEmail,
        string subject,
        string status,
        string? errorDetail) =>
        new()
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = idempotencyKey,
            EventType = eventType,
            AggregateId = aggregateId,
            RecipientEmail = recipientEmail,
            Subject = subject,
            Status = status,
            ErrorDetail = errorDetail,
            CreatedAtUtc = DateTime.UtcNow
        };

    private async Task AddLogSafeAsync(NotificationSendLog log, CancellationToken cancellationToken)
    {
        db.NotificationSendLogs.Add(log);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            logger.LogInformation("Duplicate notification log skipped (unique key) {Key}", log.IdempotencyKey);
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        var msg = ex.InnerException?.Message ?? ex.Message;
        return msg.Contains("23505", StringComparison.Ordinal)
               || msg.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase)
               || msg.Contains("duplicate key", StringComparison.OrdinalIgnoreCase);
    }

    private static (string Status, string? ErrorDetail) MapOutcome(EmailSendOutcome outcome) =>
        outcome switch
        {
            EmailSendOutcome.Delivered => (NotificationDispatchStatus.Sent, null),
            EmailSendOutcome.SkippedNotConfigured => (NotificationDispatchStatus.SkippedNotConfigured, null),
            EmailSendOutcome.ProviderRejected => (NotificationDispatchStatus.Failed, "SendGrid returned non-success status"),
            _ => (NotificationDispatchStatus.Failed, null)
        };
}
