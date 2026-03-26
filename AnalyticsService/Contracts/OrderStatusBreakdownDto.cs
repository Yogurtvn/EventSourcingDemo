namespace AnalyticsService.Contracts
{
    public sealed class OrderStatusBreakdownDto
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
