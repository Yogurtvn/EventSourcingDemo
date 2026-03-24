using System.ComponentModel.DataAnnotations;

namespace PaymentService.Models;

public sealed class PaymentReadModel
{
    [Key]
    public Guid OrderId { get; set; }

    public Guid? PaymentId { get; set; }

    public decimal Amount { get; set; }

    [MaxLength(50)]
    public required string Status { get; set; }

    [MaxLength(250)]
    public string? FailureReason { get; set; }

    public DateTime LastUpdatedAt { get; set; }
}
