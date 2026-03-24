namespace OrderService.Contracts;

public sealed class PaymentSucceededMessage
{
    public Guid OrderId { get; init; }
    public Guid PaymentId { get; init; }
    public decimal Amount { get; init; }
}
