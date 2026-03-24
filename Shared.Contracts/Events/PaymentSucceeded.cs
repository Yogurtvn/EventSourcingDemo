namespace Shared.Contracts.Events;

public sealed record PaymentSucceeded(Guid AggregateId, Guid PaymentId, decimal Amount) : Event(AggregateId);
