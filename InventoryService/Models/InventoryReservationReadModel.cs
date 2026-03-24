using System.ComponentModel.DataAnnotations;

namespace InventoryService.Models;

public sealed class InventoryReservationReadModel
{
    [Key]
    public Guid OrderId { get; set; }

    [MaxLength(50)]
    public required string ReservationStatus { get; set; }

    [MaxLength(250)]
    public string? Reason { get; set; }

    public DateTime? ReservedAt { get; set; }

    public DateTime? ReleasedAt { get; set; }

    public DateTime LastUpdatedAt { get; set; }
}
