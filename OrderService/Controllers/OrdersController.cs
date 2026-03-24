using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Contracts;
using OrderService.Infrastructure.Persistence;
using OrderService.Services;

namespace OrderService.Controllers;

[ApiController]
[Route("api/orders")]
[Route("orders")]
public sealed class OrdersController(OrderEventService orderEventService, OrderDbContext dbContext) : ControllerBase
{
    [HttpGet("customers")]
    public async Task<IActionResult> GetCustomers(CancellationToken cancellationToken)
    {
        var customers = await dbContext.Customers
            .OrderBy(x => x.CustomerId)
            .Select(x => new
            {
                x.CustomerId,
                x.FullName,
                x.Email,
                x.IsActive,
                x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(customers);
    }

    [HttpGet("recent")]
    public async Task<IActionResult> GetRecentOrders([FromQuery] int take = 20, CancellationToken cancellationToken = default)
    {
        var safeTake = Math.Clamp(take, 1, 200);
        var orders = await dbContext.Orders
            .OrderByDescending(x => x.LastUpdatedAt)
            .Take(safeTake)
            .ToListAsync(cancellationToken);

        return Ok(orders);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerId) || request.TotalAmount <= 0)
        {
            return BadRequest("CustomerId is required and TotalAmount must be greater than zero.");
        }

        var customerExists = await dbContext.Customers
            .AnyAsync(x => x.CustomerId == request.CustomerId && x.IsActive, cancellationToken);
        if (!customerExists)
        {
            return BadRequest("CustomerId does not exist or is inactive. Use GET /orders/customers to get valid IDs.");
        }

        var orderId = await orderEventService.CreateOrderAsync(request, cancellationToken);
        return Accepted(new { OrderId = orderId, Status = "Created" });
    }

    [HttpGet("{orderId:guid}")]
    public async Task<IActionResult> GetOrder(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.FirstOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        return Ok(order);
    }
}
