using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using System.Text.Json;

namespace WebFrontend.Pages
{
    public class OrderSimulatorModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public OrderSimulatorModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // Danh sách khách hàng ?? ??a vào Dropdown
        public List<CustomerDto> Customers { get; set; } = new();

        // Bi?n h?ng d? li?u t? Form g?i lên
        [BindProperty]
        public CreateOrderRequestDto OrderRequest { get; set; } = new();

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            await LoadCustomers();
        }

        // Hàm ch?y khi b?n b?m nút "KÍCH HO?T SAGA"
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadCustomers();
                return Page();
            }

            // G?i qua API Gateway ?? t?o ??n hàng
            var client = _httpClientFactory.CreateClient("GatewayClient");

            try
            {
                var content = new StringContent(JsonSerializer.Serialize(OrderRequest), Encoding.UTF8, "application/json");

                // B?n HTTP POST t?o ??n
                var response = await client.PostAsync("/api/orders", content);

                if (response.IsSuccessStatusCode)
                {
                    SuccessMessage = $"Kh?i t?o Saga thành công! Vui lòng qua trang 'Qu?n lý ??n hàng' ?? xem tr?ng thái ng?m ?ang ch?y.";
                }
                else
                {
                    ErrorMessage = $"L?i t? Gateway/OrderService: Mã tr? v? {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"L?i k?t n?i: {ex.Message}";
            }

            // Load l?i danh sách khách hàng ?? form không b? tr?ng
            await LoadCustomers();
            return Page();
        }

        private async Task LoadCustomers()
        {
            var client = _httpClientFactory.CreateClient("GatewayClient");
            try
            {
                // G?i API l?y danh sách khách hàng
                var response = await client.GetAsync("/api/orders/customers");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    Customers = JsonSerializer.Deserialize<List<CustomerDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }
                else
                {
                    ErrorMessage = $"Không th? t?i danh sách khách hàng. Mã l?i: {response.StatusCode}. (L?u ý: N?u b? 404, hãy ki?m tra YARP Gateway có map ?úng route ch?a!)";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"L?i k?t n?i t?i khách hàng: {ex.Message}";
            }
        }
    }

    // --- Các DTO h?ng d? li?u ---
    public class CustomerDto
    {
        public Guid CustomerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class CreateOrderRequestDto
    {
        public Guid CustomerId { get; set; }
        public decimal TotalAmount { get; set; } = 500000;
        public string TestScenario { get; set; } = "happy"; // M?c ??nh là thành công
    }
}