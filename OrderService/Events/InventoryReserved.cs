namespace OrderService.Events;

public sealed record InventoryReserved(Guid AggregateId, DateTime ReservedUntil) : Event(AggregateId);
