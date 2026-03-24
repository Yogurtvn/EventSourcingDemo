using Microsoft.AspNetCore.Mvc;
using OrderService.Contracts;
using Shared.Contracts.Events;
using Shared.Messaging.RabbitMq;

namespace OrderService.Controllers;

[ApiController]
[Route("api/order-events")]
public sealed class OrderEventsController(IRabbitMqEventBus eventBus) : ControllerBase
{
    [HttpPost("payment-succeeded")]
    public async Task<IActionResult> PaymentSucceeded([FromBody] PaymentSucceededMessage message, CancellationToken cancellationToken)
    {
        var @event = new PaymentSucceeded(message.OrderId, message.PaymentId, message.Amount);
        await eventBus.PublishAsync(@event, cancellationToken);

        return Accepted();
    }

    [HttpPost("payment-failed")]
    public async Task<IActionResult> PaymentFailed([FromBody] PaymentFailedMessage message, CancellationToken cancellationToken)
    {
        var @event = new PaymentFailed(message.OrderId, message.Reason);
        await eventBus.PublishAsync(@event, cancellationToken);

        return Accepted();
    }
}
