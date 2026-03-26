using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace WebFrontend.Pages;

public class IndexModel(IHttpClientFactory httpClientFactory) : PageModel
{
    public DashboardSummaryDto? Summary { get; set; }
    public List<TopSpenderDto> TopSpenders { get; set; } = new();
    public string ErrorMessage { get; set; } = string.Empty;

    public async Task OnGetAsync()
    {
        var client = httpClientFactory.CreateClient("AnalyticsClient");

        try
        {
            using var summaryResponse = await client.GetAsync("/api/AnalyticsDashboard/summary");
            if (summaryResponse.IsSuccessStatusCode)
            {
                Summary = await summaryResponse.Content.ReadFromJsonAsync<DashboardSummaryDto>();
            }
            else
            {
                ErrorMessage += $"Dashboard API returned {summaryResponse.StatusCode}. ";
            }

            using var topSpendersResponse = await client.GetAsync("/api/AnalyticsDashboard/customers/top-spenders?take=5");
            if (topSpendersResponse.IsSuccessStatusCode)
            {
                TopSpenders = await topSpendersResponse.Content.ReadFromJsonAsync<List<TopSpenderDto>>() ?? new();
            }
            else
            {
                ErrorMessage += $"Top spenders API returned {topSpendersResponse.StatusCode}. ";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not connect to AnalyticsService. Error: {ex.Message}";
        }
    }
}

public class DashboardSummaryDto
{
    public int TotalOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int CancelledOrders { get; set; }
    public int PendingOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalCustomers { get; set; }
    public decimal AverageOrderValue { get; set; }

    public double SuccessRate => TotalOrders == 0
        ? 0
        : Math.Round((double)CompletedOrders / TotalOrders * 100, 1);
}

public class TopSpenderDto
{
    public string CustomerId { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
}
