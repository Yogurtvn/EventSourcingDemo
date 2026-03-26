using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using WebFrontend.Models;

namespace WebFrontend.Pages;

public class IndexModel(IHttpClientFactory httpClientFactory) : PageModel
{
    public DashboardSummaryDto? Summary { get; set; }
    public List<TopSpenderDto> TopSpenders { get; set; } = new();
    public string ErrorMessage { get; set; } = string.Empty;

    public async Task OnGetAsync()
    {
        var analyticsClient = httpClientFactory.CreateClient("AnalyticsClient");
        var gatewayClient = httpClientFactory.CreateClient("GatewayClient");

        try
        {
            using var summaryResponse = await analyticsClient.GetAsync("/api/AnalyticsDashboard/summary");
            if (summaryResponse.IsSuccessStatusCode)
            {
                Summary = await summaryResponse.Content.ReadFromJsonAsync<DashboardSummaryDto>();
            }
            else
            {
                ErrorMessage += $"API dashboard trả về {summaryResponse.StatusCode}. ";
            }

            using var topSpendersResponse = await analyticsClient.GetAsync("/api/AnalyticsDashboard/customers/top-spenders?take=5");
            if (topSpendersResponse.IsSuccessStatusCode)
            {
                TopSpenders = await topSpendersResponse.Content.ReadFromJsonAsync<List<TopSpenderDto>>() ?? new();
            }
            else
            {
                ErrorMessage += $"API top spender trả về {topSpendersResponse.StatusCode}. ";
            }

            using var customersResponse = await gatewayClient.GetAsync("/orders/customers");
            if (customersResponse.IsSuccessStatusCode)
            {
                var customers = await customersResponse.Content.ReadFromJsonAsync<List<CustomerCatalogItemDto>>() ?? new();
                var customerLookup = customers.ToDictionary(x => x.CustomerId, x => x.FullName, StringComparer.OrdinalIgnoreCase);

                foreach (var spender in TopSpenders)
                {
                    if (customerLookup.TryGetValue(spender.CustomerId, out var fullName) &&
                        !string.IsNullOrWhiteSpace(fullName))
                    {
                        spender.CustomerName = fullName;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể kết nối tới AnalyticsService. Lỗi: {ex.Message}";
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
    public string? CustomerName { get; set; }

    public string DisplayCustomerName => string.IsNullOrWhiteSpace(CustomerName)
        ? CustomerId
        : CustomerName;
}
