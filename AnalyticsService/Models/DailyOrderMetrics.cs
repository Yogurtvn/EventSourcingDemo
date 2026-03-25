using System.ComponentModel.DataAnnotations;

namespace AnalyticsService.Models;

public sealed class DailyOrderMetrics
{
    [Key]
    public DateTime Date { get; set; }

    public int TotalOrdersCreated { get; set; }

    public int TotalOrdersCompleted { get; set; }

    public int TotalOrdersCancelled { get; set; }

    public decimal TotalRevenue { get; set; }

    public decimal AverageOrderValue { get; set; }

    public double CompletionRate { get; set; }
}
