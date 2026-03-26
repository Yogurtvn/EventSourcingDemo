namespace AnalyticsService.Contracts
{
    public sealed class AnalyticsDashboardSummaryDto
    {
        public int TotalOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }
        public int PendingOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalCustomers { get; set; }
        public decimal AverageOrderValue { get; set; }
    }
}
