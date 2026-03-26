using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using WebFrontend.Models;

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
            using var ordersResponse = await client.GetAsync("/orders/recent?take=50");
            if (!ordersResponse.IsSuccessStatusCode)
            {
                ErrorMessage = $"API danh sách đơn hàng trả về {ordersResponse.StatusCode}.";
                return;
            }

            Orders = await ordersResponse.Content.ReadFromJsonAsync<List<OrderListItemDto>>() ?? new();

            using var customersResponse = await client.GetAsync("/orders/customers");
            if (!customersResponse.IsSuccessStatusCode)
            {
                return;
            }

            var customers = await customersResponse.Content.ReadFromJsonAsync<List<CustomerCatalogItemDto>>() ?? new();
            var customerLookup = customers.ToDictionary(x => x.CustomerId, x => x.FullName, StringComparer.OrdinalIgnoreCase);

            foreach (var order in Orders)
            {
                if (customerLookup.TryGetValue(order.CustomerId, out var fullName) &&
                    !string.IsNullOrWhiteSpace(fullName))
                {
                    order.CustomerName = fullName;
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể tải danh sách đơn hàng. Lỗi: {ex.Message}";
        }
    }

    public static string GetStatusClass(string? status) => status switch
    {
        "Completed" => "is-success",
        "Cancelled" => "is-danger",
        "PaymentFailed" => "is-danger",
        "InventoryFailed" => "is-danger",
        "PaymentSucceeded" => "is-info",
        "Created" => "is-warning",
        _ => "is-neutral"
    };

    public static string GetDisplayStatus(string? status) => status switch
    {
        "Created" => "Đang xử lý",
        "Completed" => "Hoàn tất",
        "Cancelled" => "Đã huỷ",
        "PaymentFailed" => "Thanh toán lỗi",
        "InventoryFailed" => "Giữ hàng lỗi",
        "PaymentSucceeded" => "Đã thanh toán",
        _ when string.IsNullOrWhiteSpace(status) => "Không rõ",
        _ => status
    };
}

public sealed class OrderListItemDto
{
    public Guid OrderId { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime LastUpdatedAt { get; set; }

    public string DisplayCustomerName => string.IsNullOrWhiteSpace(CustomerName)
        ? CustomerId
        : CustomerName;
}
