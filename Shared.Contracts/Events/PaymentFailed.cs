namespace Shared.Contracts.Events;

public sealed record PaymentFailed(Guid AggregateId, string Reason) : Event(AggregateId);
