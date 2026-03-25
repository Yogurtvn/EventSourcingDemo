using System.ComponentModel.DataAnnotations;

namespace NotificationService.Models;

public sealed class NotificationSendLog
{
    public Guid Id { get; set; }

    [MaxLength(320)]
    public required string IdempotencyKey { get; set; }

    [MaxLength(80)]
    public required string EventType { get; set; }

    public Guid AggregateId { get; set; }

    [MaxLength(500)]
    public string? RecipientEmail { get; set; }

    [MaxLength(500)]
    public required string Subject { get; set; }

    [MaxLength(50)]
    public required string Status { get; set; }

    [MaxLength(2000)]
    public string? ErrorDetail { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
