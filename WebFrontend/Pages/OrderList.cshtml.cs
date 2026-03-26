using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace WebFrontend.Pages;

public class OrderListModel(IHttpClientFactory httpClientFactory) : PageModel
{
    public List<OrderListItemDto> Orders { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        var client = httpClientFactory.CreateClient("GatewayClient");

        try
        {
            using var response = await client.GetAsync("/orders/recent?take=50");
            if (response.IsSuccessStatusCode)
            {
                Orders = await response.Content.ReadFromJsonAsync<List<OrderListItemDto>>() ?? new();
                return;
            }

            ErrorMessage = $"Order list API returned {response.StatusCode}.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not load orders. Error: {ex.Message}";
        }
    }

    public static string GetStatusBadgeClass(string? status) => status switch
    {
        "Completed" => "bg-success",
        "Cancelled" => "bg-danger",
        "PaymentFailed" => "bg-danger",
        "InventoryFailed" => "bg-danger",
        "PaymentSucceeded" => "bg-info text-dark",
        "Created" => "bg-warning text-dark",
        _ => "bg-secondary"
    };

    public static string GetDisplayStatus(string? status) => status switch
    {
        "Created" => "Pending",
        _ when string.IsNullOrWhiteSpace(status) => "Unknown",
        _ => status
    };
}

public sealed class OrderListItemDto
{
    public Guid OrderId { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime LastUpdatedAt { get; set; }
}
