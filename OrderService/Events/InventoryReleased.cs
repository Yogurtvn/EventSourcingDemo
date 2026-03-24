namespace OrderService.Events;

public sealed record InventoryReleased(Guid AggregateId, string Reason) : Event(AggregateId);
