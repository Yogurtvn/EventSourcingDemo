namespace AnalyticsService.Contracts
{
    public sealed class TopSpenderDto
    {
        public string CustomerId { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }
    }
}
