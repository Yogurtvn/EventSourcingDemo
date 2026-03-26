using AnalyticsService.Contracts;
using AnalyticsService.Infrastructure.Persistence;
using AnalyticsService.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnalyticsService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class AnalyticsDashboardController(AnalyticsDbContext dbContext) : ControllerBase
    {
        // <summary>
        // Lấy dữ liệu tổng quan cho dashboard analytics.
        //tổng số đơn hàng
        //số đơn hoàn thành
        //số đơn đã hủy
        //số đơn đang chờ
        //tổng doanh thu
        //tổng số khách hàng
        //giá trị đơn hàng trung bình
        // </summary>
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
        {
            var totalOrders = await dbContext.OrderAnalytics.CountAsync(cancellationToken);
            var completedOrders = await dbContext.OrderAnalytics.CountAsync(x => x.Status == "Completed", cancellationToken);
            var cancelledOrders = await dbContext.OrderAnalytics.CountAsync(x => x.Status == "Cancelled", cancellationToken);
            var pendingOrders = await dbContext.OrderAnalytics.CountAsync(x => x.Status == "Pending", cancellationToken);

            var totalRevenue = await dbContext.CustomerAnalytics
                .SumAsync(x => (decimal?)x.TotalSpent, cancellationToken) ?? 0m;

            var totalCustomers = await dbContext.CustomerAnalytics.CountAsync(cancellationToken);

            var averageOrderValue = totalOrders == 0
                ? 0m
                : totalRevenue / totalOrders;

            var result = new AnalyticsDashboardSummaryDto
            {
                TotalOrders = totalOrders,
                CompletedOrders = completedOrders,
                CancelledOrders = cancelledOrders,
                PendingOrders = pendingOrders,
                TotalRevenue = totalRevenue,
                TotalCustomers = totalCustomers,
                AverageOrderValue = averageOrderValue
            };

            return Ok(result);
        }

        // <summary>
        // Lấy danh sách khách hàng chi tiêu cao nhất.
        // </summary>
        [HttpGet("customers/top-spenders")]
        public async Task<IActionResult> GetTopSpenders([FromQuery] int take = 10, CancellationToken cancellationToken = default)
        {
            var safeTake = Math.Clamp(take, 1, 100);

            var result = await dbContext.CustomerAnalytics
                .OrderByDescending(x => x.TotalSpent)
                .ThenByDescending(x => x.TotalOrders)
                .ThenBy(x => x.CustomerId)
                .Take(safeTake)
                .Select(x => new TopSpenderDto
                {
                    CustomerId = x.CustomerId,
                    TotalOrders = x.TotalOrders,
                    TotalSpent = x.TotalSpent
                })
                .ToListAsync(cancellationToken);

            return Ok(result);
        }

        // <summary>
        // Thống kê số lượng đơn hàng theo từng trạng thái.
        // </summary>
        [HttpGet("orders/status-breakdown")]
        public async Task<IActionResult> GetStatusBreakdown(CancellationToken cancellationToken)
        {
            var result = await dbContext.OrderAnalytics
                .GroupBy(x => x.Status)
                .Select(g => new OrderStatusBreakdownDto
                {
                    Status = g.Key ?? "Unknown",
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Status)
                .ToListAsync(cancellationToken);

            return Ok(result);
        }
    }
}
