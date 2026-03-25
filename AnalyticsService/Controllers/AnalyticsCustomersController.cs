using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AnalyticsService.Infrastructure.Persistence;
using AnalyticsService.Models;

namespace AnalyticsService.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AnalyticsCustomersController(
    AnalyticsDbContext dbContext,
    ILogger<AnalyticsCustomersController> logger) : ControllerBase
{
    /// <summary>
    /// Get all customer analytics
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerAnalyticsReadModel>>> GetAllCustomerAnalytics(
        CancellationToken cancellationToken)
    {
        try
        {
            var customers = await dbContext.CustomerAnalytics
                .OrderByDescending(x => x.TotalSpent)
                .ToListAsync(cancellationToken);

            logger.LogInformation("Retrieved analytics for {Count} customers", customers.Count);
            return Ok(customers);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving customer analytics");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get top customers by spending
    /// </summary>
    [HttpGet("top-spenders")]
    public async Task<ActionResult<IEnumerable<CustomerAnalyticsReadModel>>> GetTopSpenders(
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var topSpenders = await dbContext.CustomerAnalytics
                .OrderByDescending(x => x.TotalSpent)
                .Take(limit)
                .ToListAsync(cancellationToken);

            logger.LogInformation("Retrieved top {Limit} spenders", limit);
            return Ok(topSpenders);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving top spenders");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get customer analytics by customer ID
    /// </summary>
    [HttpGet("{customerId}")]
    public async Task<ActionResult<CustomerAnalyticsReadModel>> GetCustomerAnalytics(
        string customerId,
        CancellationToken cancellationToken)
    {
        try
        {
            var customer = await dbContext.CustomerAnalytics
                .FirstOrDefaultAsync(x => x.CustomerId == customerId, cancellationToken);

            if (customer == null)
            {
                logger.LogWarning("Customer analytics not found for CustomerId {CustomerId}", customerId);
                return NotFound();
            }

            logger.LogInformation("Retrieved customer analytics for CustomerId {CustomerId}", customerId);
            return Ok(customer);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving customer analytics");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get customer orders analytics
    /// </summary>
    [HttpGet("{customerId}/orders")]
    public async Task<ActionResult<IEnumerable<OrderAnalyticsReadModel>>> GetCustomerOrders(
        string customerId,
        CancellationToken cancellationToken)
    {
        try
        {
            var orders = await dbContext.OrderAnalytics
                .Where(x => x.CustomerId == customerId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);

            logger.LogInformation("Retrieved {Count} orders for CustomerId {CustomerId}", orders.Count, customerId);
            return Ok(orders);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving customer orders");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
