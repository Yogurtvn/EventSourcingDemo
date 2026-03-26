using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace WebFrontend.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public DashboardSummaryDto? Summary { get; set; }
        public List<TopSpenderDto> TopSpenders { get; set; } = new();
        public string ErrorMessage { get; set; } = "";

        public IndexModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task OnGetAsync()
        {
            // Dùng client gọi thẳng vào cổng của AnalyticsService (Ví dụ: http://localhost:5005)
            var client = _httpClientFactory.CreateClient("AnalyticsClient");

            try
            {
                // 1. Gọi API Tổng quan (Map theo tên class AnalyticsDashboardController)
                var summaryRes = await client.GetAsync("/api/AnalyticsDashboard");

                if (summaryRes.IsSuccessStatusCode)
                {
                    var json = await summaryRes.Content.ReadAsStringAsync();
                    Summary = JsonSerializer.Deserialize<DashboardSummaryDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                else
                {
                    ErrorMessage += $"Lỗi Dashboard: API /api/AnalyticsDashboard trả về {summaryRes.StatusCode}. ";
                }

                // 2. Gọi API Top Khách Hàng (Map theo class AnalyticsCustomersController)
                var spendersRes = await client.GetAsync("/api/AnalyticsCustomers/top-spenders?take=5");

                if (spendersRes.IsSuccessStatusCode)
                {
                    var json = await spendersRes.Content.ReadAsStringAsync();
                    TopSpenders = JsonSerializer.Deserialize<List<TopSpenderDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }
                else
                {
                    ErrorMessage += $"Lỗi Top Spender: API /api/AnalyticsCustomers/top-spenders trả về {spendersRes.StatusCode}. ";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Không thể kết nối đến AnalyticsService. Lỗi: " + ex.Message;
            }
        }
    }

    // --- Giữ nguyên các class DTO bên dưới ---
    public class DashboardSummaryDto
    {
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int SuccessfulOrders { get; set; }
        public int FailedOrders { get; set; }
        public double SuccessRate { get; set; }
    }

    public class TopSpenderDto
    {
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalSpent { get; set; }
        public int OrderCount { get; set; }
    }
}