namespace Shared.Contracts.Events;

public sealed record InventoryFailed(Guid AggregateId, string Reason) : Event(AggregateId);
