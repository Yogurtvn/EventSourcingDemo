using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaymentService.Infrastructure.Persistence;

namespace PaymentService.Controllers;

[ApiController]
[Route("api/payments")]
public sealed class PaymentQueryController(PaymentDbContext dbContext) : ControllerBase
{
    [HttpGet("{orderId:guid}")]
    public async Task<IActionResult> GetByOrderId(Guid orderId, CancellationToken cancellationToken)
    {
        var model = await dbContext.Payments.FirstOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);
        return model is null ? NotFound() : Ok(model);
    }
}
