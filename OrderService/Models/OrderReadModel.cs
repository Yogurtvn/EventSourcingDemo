using System.ComponentModel.DataAnnotations;

namespace OrderService.Models;

public sealed class OrderReadModel
{
    [Key]
    public Guid OrderId { get; set; }

    [MaxLength(150)]
    public required string CustomerId { get; set; }

    public decimal TotalAmount { get; set; }

    [MaxLength(50)]
    public required string Status { get; set; }

    public DateTime LastUpdatedAt { get; set; }
}
