using System.ComponentModel.DataAnnotations;

namespace AnalyticsService.Models;

public sealed class CustomerAnalyticsReadModel
{
    [Key]
    [MaxLength(150)]
    public required string CustomerId { get; set; }

    public int TotalOrders { get; set; }

    public int CompletedOrders { get; set; }

    public int CancelledOrders { get; set; }

    public decimal TotalSpent { get; set; }

    public decimal AverageOrderValue { get; set; }

    public DateTime LastOrderDate { get; set; }

    public DateTime FirstOrderDate { get; set; }
}
