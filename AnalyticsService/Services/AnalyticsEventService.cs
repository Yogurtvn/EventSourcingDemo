using Microsoft.EntityFrameworkCore;
using AnalyticsService.Infrastructure.Persistence;
using AnalyticsService.Models;
using Shared.Contracts.Events;

namespace AnalyticsService.Services;

public sealed class AnalyticsEventService(
    AnalyticsDbContext dbContext,
    ILogger<AnalyticsEventService> logger)
{
    public async Task HandleOrderCreatedAsync(OrderCreated @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var orderAnalytics = new OrderAnalyticsReadModel
            {
                OrderId = @event.AggregateId,
                CustomerId = @event.CustomerId,
                TotalAmount = @event.TotalAmount,
                Status = "Created",
                CreatedAt = @event.Timestamp
            };

            dbContext.OrderAnalytics.Add(orderAnalytics);

            // Update customer analytics
            var customerAnalytics = await dbContext.CustomerAnalytics
                .FirstOrDefaultAsync(x => x.CustomerId == @event.CustomerId, cancellationToken);

            if (customerAnalytics == null)
            {
                customerAnalytics = new CustomerAnalyticsReadModel
                {
                    CustomerId = @event.CustomerId,
                    TotalOrders = 1,
                    CompletedOrders = 0,
                    CancelledOrders = 0,
                    TotalSpent = @event.TotalAmount,
                    AverageOrderValue = @event.TotalAmount,
                    FirstOrderDate = @event.Timestamp,
                    LastOrderDate = @event.Timestamp
                };
                dbContext.CustomerAnalytics.Add(customerAnalytics);
            }
            else
            {
                customerAnalytics.TotalOrders++;
                customerAnalytics.TotalSpent += @event.TotalAmount;
                customerAnalytics.AverageOrderValue = customerAnalytics.TotalSpent / customerAnalytics.TotalOrders;
                customerAnalytics.LastOrderDate = @event.Timestamp;
            }

            // Update daily metrics
            await UpdateDailyMetricsAsync(@event.Timestamp, cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("OrderCreated analytics recorded for OrderId {OrderId}", @event.AggregateId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling OrderCreated event for OrderId {OrderId}", @event.AggregateId);
            throw;
        }
    }

    public async Task HandleOrderCompletedAsync(OrderCompleted @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var orderAnalytics = await dbContext.OrderAnalytics
                .FirstOrDefaultAsync(x => x.OrderId == @event.AggregateId, cancellationToken);

            if (orderAnalytics != null)
            {
                orderAnalytics.Status = "Completed";
                orderAnalytics.CompletedAt = @event.Timestamp;
            }

            // Update customer analytics
            var customerAnalytics = await dbContext.CustomerAnalytics
                .FirstOrDefaultAsync(x => x.CustomerId == orderAnalytics!.CustomerId, cancellationToken);

            if (customerAnalytics != null)
            {
                customerAnalytics.CompletedOrders++;
            }

            // Update daily metrics
            await UpdateDailyMetricsAsync(@event.Timestamp, cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("OrderCompleted analytics recorded for OrderId {OrderId}", @event.AggregateId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling OrderCompleted event for OrderId {OrderId}", @event.AggregateId);
            throw;
        }
    }

    public async Task HandleOrderCancelledAsync(OrderCancelled @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var orderAnalytics = await dbContext.OrderAnalytics
                .FirstOrDefaultAsync(x => x.OrderId == @event.AggregateId, cancellationToken);

            if (orderAnalytics != null)
            {
                orderAnalytics.Status = "Cancelled";
                orderAnalytics.CancelledAt = @event.Timestamp;
            }

            // Update customer analytics
            var customerAnalytics = await dbContext.CustomerAnalytics
                .FirstOrDefaultAsync(x => x.CustomerId == orderAnalytics!.CustomerId, cancellationToken);

            if (customerAnalytics != null)
            {
                customerAnalytics.CancelledOrders++;
            }

            // Update daily metrics
            await UpdateDailyMetricsAsync(@event.Timestamp, cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("OrderCancelled analytics recorded for OrderId {OrderId}", @event.AggregateId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling OrderCancelled event for OrderId {OrderId}", @event.AggregateId);
            throw;
        }
    }

    private async Task UpdateDailyMetricsAsync(DateTime timestamp, CancellationToken cancellationToken)
    {
        var date = timestamp.Date;
        var dailyMetrics = await dbContext.DailyMetrics
            .FirstOrDefaultAsync(x => x.Date == date, cancellationToken);

        if (dailyMetrics == null)
        {
            dailyMetrics = new DailyOrderMetrics
            {
                Date = date,
                TotalOrdersCreated = 0,
                TotalOrdersCompleted = 0,
                TotalOrdersCancelled = 0,
                TotalRevenue = 0,
                AverageOrderValue = 0,
                CompletionRate = 0
            };
            dbContext.DailyMetrics.Add(dailyMetrics);
        }

        // Recalculate metrics for the day
        var ordersForDay = await dbContext.OrderAnalytics
            .Where(x => x.CreatedAt.Date == date)
            .ToListAsync(cancellationToken);

        dailyMetrics.TotalOrdersCreated = ordersForDay.Count;
        dailyMetrics.TotalOrdersCompleted = ordersForDay.Count(x => x.Status == "Completed");
        dailyMetrics.TotalOrdersCancelled = ordersForDay.Count(x => x.Status == "Cancelled");
        dailyMetrics.TotalRevenue = ordersForDay.Sum(x => x.TotalAmount);
        dailyMetrics.AverageOrderValue = ordersForDay.Any() ? ordersForDay.Average(x => x.TotalAmount) : 0;
        dailyMetrics.CompletionRate = ordersForDay.Any() 
            ? (double)dailyMetrics.TotalOrdersCompleted / ordersForDay.Count 
            : 0;
    }
}
