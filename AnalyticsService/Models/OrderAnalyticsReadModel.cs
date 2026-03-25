using System.ComponentModel.DataAnnotations;

namespace AnalyticsService.Models;

public sealed class OrderAnalyticsReadModel
{
    [Key]
    public Guid OrderId { get; set; }

    [MaxLength(150)]
    public required string CustomerId { get; set; }

    public decimal TotalAmount { get; set; }

    [MaxLength(50)]
    public required string Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public int DaysToCompletion => CompletedAt.HasValue ? (int)(CompletedAt.Value - CreatedAt).TotalDays : -1;
}
