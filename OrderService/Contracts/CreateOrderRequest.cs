namespace OrderService.Contracts;

public sealed class CreateOrderRequest
{
    public required string CustomerId { get; init; }
    public decimal TotalAmount { get; init; }
    public string? TestScenario { get; init; }
}
