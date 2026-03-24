using Microsoft.EntityFrameworkCore;
using PaymentService.Infrastructure.Persistence;
using PaymentService.Models;
using Shared.Contracts.Events;

namespace PaymentService.Services;

public sealed class PaymentProjector(PaymentDbContext dbContext)
{
    public async Task ApplyAsync(Event @event, CancellationToken cancellationToken = default)
    {
        switch (@event)
        {
            case PaymentSucceeded succeeded:
                await UpsertAsync(succeeded.AggregateId, succeeded.PaymentId, succeeded.Amount, "Succeeded", null, succeeded.Timestamp, cancellationToken);
                break;
            case PaymentFailed failed:
                await UpsertAsync(failed.AggregateId, null, 0, "Failed", failed.Reason, failed.Timestamp, cancellationToken);
                break;
        }
    }

    private async Task UpsertAsync(Guid orderId, Guid? paymentId, decimal amount, string status, string? reason, DateTime updatedAt, CancellationToken cancellationToken)
    {
        var model = await dbContext.Payments.FirstOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);
        if (model is null)
        {
            model = new PaymentReadModel
            {
                OrderId = orderId,
                PaymentId = paymentId,
                Amount = amount,
                Status = status,
                FailureReason = reason,
                LastUpdatedAt = updatedAt
            };
            dbContext.Payments.Add(model);
        }
        else
        {
            model.PaymentId = paymentId ?? model.PaymentId;
            model.Amount = amount == 0 ? model.Amount : amount;
            model.Status = status;
            model.FailureReason = reason;
            model.LastUpdatedAt = updatedAt;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
