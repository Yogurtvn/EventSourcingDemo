namespace OrderService.Contracts;

public sealed class PaymentFailedMessage
{
    public Guid OrderId { get; init; }
    public string Reason { get; init; } = "Unknown";
}
