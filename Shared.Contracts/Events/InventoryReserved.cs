namespace Shared.Contracts.Events;

public sealed record InventoryReserved(Guid AggregateId, DateTime ReservedUntil, string? TestScenario = null) : Event(AggregateId);
