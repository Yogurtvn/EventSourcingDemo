using System.ComponentModel.DataAnnotations;

namespace OrderService.Models;

public sealed class EventStoreEntry
{
    [Key]
    public long Id { get; set; }

    public Guid AggregateId { get; set; }

    [MaxLength(200)]
    public required string EventType { get; set; }

    public required string EventData { get; set; }

    public DateTime Timestamp { get; set; }

    public int Version { get; set; }
}
