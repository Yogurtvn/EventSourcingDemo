using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AnalyticsService.Infrastructure.Persistence;
using AnalyticsService.Models;

namespace AnalyticsService.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AnalyticsMetricsController(
    AnalyticsDbContext dbContext,
    ILogger<AnalyticsMetricsController> logger) : ControllerBase
{
    /// <summary>
    /// Get daily metrics for a specific date
    /// </summary>
    [HttpGet("daily/{date}")]
    public async Task<ActionResult<DailyOrderMetrics>> GetDailyMetrics(
        [FromRoute] DateTime date,
        CancellationToken cancellationToken)
    {
        try
        {
            var metrics = await dbContext.DailyMetrics
                .FirstOrDefaultAsync(x => x.Date == date.Date, cancellationToken);

            if (metrics == null)
            {
                logger.LogWarning("Daily metrics not found for date {Date}", date);
                return NotFound();
            }

            logger.LogInformation("Retrieved daily metrics for date {Date}", date);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving daily metrics");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get daily metrics for a date range
    /// </summary>
    [HttpGet("daily-range")]
    public async Task<ActionResult<IEnumerable<DailyOrderMetrics>>> GetDailyMetricsRange(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken)
    {
        try
        {
            var metrics = await dbContext.DailyMetrics
                .Where(x => x.Date >= startDate.Date && x.Date <= endDate.Date)
                .OrderBy(x => x.Date)
                .ToListAsync(cancellationToken);

            logger.LogInformation("Retrieved {Count} daily metrics for range {StartDate} - {EndDate}", 
                metrics.Count, startDate, endDate);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving daily metrics range");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get aggregated metrics for a date range
    /// </summary>
    [HttpGet("aggregated-range")]
    public async Task<ActionResult<AggregatedMetricsDto>> GetAggregatedMetrics(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken)
    {
        try
        {
            var dailyMetrics = await dbContext.DailyMetrics
                .Where(x => x.Date >= startDate.Date && x.Date <= endDate.Date)
                .ToListAsync(cancellationToken);

            if (!dailyMetrics.Any())
            {
                logger.LogWarning("No metrics found for date range {StartDate} - {EndDate}", startDate, endDate);
                return NotFound();
            }

            var aggregated = new AggregatedMetricsDto
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalOrdersCreated = dailyMetrics.Sum(x => x.TotalOrdersCreated),
                TotalOrdersCompleted = dailyMetrics.Sum(x => x.TotalOrdersCompleted),
                TotalOrdersCancelled = dailyMetrics.Sum(x => x.TotalOrdersCancelled),
                TotalRevenue = dailyMetrics.Sum(x => x.TotalRevenue),
                AverageOrderValue = dailyMetrics.Average(x => x.AverageOrderValue),
                AverageCompletionRate = dailyMetrics.Average(x => x.CompletionRate),
                DaysInRange = dailyMetrics.Count
            };

            logger.LogInformation("Retrieved aggregated metrics for range {StartDate} - {EndDate}", startDate, endDate);
            return Ok(aggregated);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving aggregated metrics");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public sealed class AggregatedMetricsDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalOrdersCreated { get; set; }
    public int TotalOrdersCompleted { get; set; }
    public int TotalOrdersCancelled { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageOrderValue { get; set; }
    public double AverageCompletionRate { get; set; }
    public int DaysInRange { get; set; }
}
