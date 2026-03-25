using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AnalyticsService.Infrastructure.Persistence;
using AnalyticsService.Models;

namespace AnalyticsService.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AnalyticsOrdersController(
    AnalyticsDbContext dbContext,
    ILogger<AnalyticsOrdersController> logger) : ControllerBase
{
    /// <summary>
    /// Get overall order statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<OrderStatisticsDto>> GetOrderStatistics(CancellationToken cancellationToken)
    {
        try
        {
            var orders = await dbContext.OrderAnalytics.ToListAsync(cancellationToken);
            var completedOrders = orders.Where(x => x.Status == "Completed").ToList();
            var cancelledOrders = orders.Where(x => x.Status == "Cancelled").ToList();

            var statistics = new OrderStatisticsDto
            {
                TotalOrders = orders.Count,
                CompletedOrders = completedOrders.Count,
                CancelledOrders = cancelledOrders.Count,
                CreatedOrders = orders.Count(x => x.Status == "Created"),
                TotalRevenue = orders.Sum(x => x.TotalAmount),
                AverageOrderValue = orders.Any() ? orders.Average(x => x.TotalAmount) : 0,
                CompletionRate = orders.Any() ? (double)completedOrders.Count / orders.Count * 100 : 0,
                CancellationRate = orders.Any() ? (double)cancelledOrders.Count / orders.Count * 100 : 0
            };

            logger.LogInformation("Order statistics retrieved");
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving order statistics");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get orders by date range
    /// </summary>
    [HttpGet("by-date-range")]
    public async Task<ActionResult<IEnumerable<OrderAnalyticsReadModel>>> GetOrdersByDateRange(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken)
    {
        try
        {
            var orders = await dbContext.OrderAnalytics
                .Where(x => x.CreatedAt >= startDate && x.CreatedAt <= endDate)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);

            logger.LogInformation("Retrieved {Count} orders for date range {StartDate} - {EndDate}", 
                orders.Count, startDate, endDate);
            return Ok(orders);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving orders by date range");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get orders by status
    /// </summary>
    [HttpGet("by-status/{status}")]
    public async Task<ActionResult<IEnumerable<OrderAnalyticsReadModel>>> GetOrdersByStatus(
        string status,
        CancellationToken cancellationToken)
    {
        try
        {
            var orders = await dbContext.OrderAnalytics
                .Where(x => x.Status == status)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);

            logger.LogInformation("Retrieved {Count} orders with status {Status}", orders.Count, status);
            return Ok(orders);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving orders by status");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get order details by ID
    /// </summary>
    [HttpGet("{orderId}")]
    public async Task<ActionResult<OrderAnalyticsReadModel>> GetOrderAnalytics(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        try
        {
            var order = await dbContext.OrderAnalytics
                .FirstOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);

            if (order == null)
            {
                logger.LogWarning("Order analytics not found for OrderId {OrderId}", orderId);
                return NotFound();
            }

            logger.LogInformation("Retrieved order analytics for OrderId {OrderId}", orderId);
            return Ok(order);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving order analytics");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public sealed class OrderStatisticsDto
{
    public int TotalOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int CancelledOrders { get; set; }
    public int CreatedOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageOrderValue { get; set; }
    public double CompletionRate { get; set; }
    public double CancellationRate { get; set; }
}
