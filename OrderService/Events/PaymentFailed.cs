namespace OrderService.Events;

public sealed record PaymentFailed(Guid AggregateId, string Reason) : Event(AggregateId);
