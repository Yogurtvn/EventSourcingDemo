using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace WebFrontend.Pages;

public class OrderSimulatorModel(IHttpClientFactory httpClientFactory) : PageModel
{
    public List<CustomerDto> Customers { get; set; } = new();

    [BindProperty]
    public CreateOrderRequestDto OrderRequest { get; set; } = new();

    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadCustomersAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadCustomersAsync();
            return Page();
        }

        var client = httpClientFactory.CreateClient("GatewayClient");

        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(OrderRequest),
                Encoding.UTF8,
                "application/json");

            using var response = await client.PostAsync("/gateway/orders", content);
            if (response.IsSuccessStatusCode)
            {
                SuccessMessage = "Khởi tạo Saga thành công. Mở trang Quản lý đơn hàng để xem kết quả.";
            }
            else
            {
                ErrorMessage = $"Gateway hoặc OrderService trả về {response.StatusCode}.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi kết nối: {ex.Message}";
        }

        await LoadCustomersAsync();
        return Page();
    }

    private async Task LoadCustomersAsync()
    {
        var client = httpClientFactory.CreateClient("GatewayClient");

        try
        {
            using var response = await client.GetAsync("/orders/customers");
            if (response.IsSuccessStatusCode)
            {
                Customers = await response.Content.ReadFromJsonAsync<List<CustomerDto>>() ?? new();
            }
            else
            {
                ErrorMessage = $"Không thể tải danh sách khách hàng. Mã lỗi: {response.StatusCode}.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi kết nối tới API khách hàng: {ex.Message}";
        }
    }
}

public class CustomerDto
{
    public string CustomerId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class CreateOrderRequestDto
{
    public string CustomerId { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; } = 500000;
    public string TestScenario { get; set; } = "happy";
}
