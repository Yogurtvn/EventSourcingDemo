namespace Shared.Contracts.Events;

public sealed record OrderCancelled(Guid AggregateId, string Reason) : Event(AggregateId);
