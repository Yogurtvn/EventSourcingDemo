using Microsoft.AspNetCore.Mvc;

namespace NotificationService.Controllers;

[ApiController]
[Route("api/notifications")]
[Route("notifications")]
public sealed class NotificationStatusController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult GetHealth() =>
        Ok(new { Status = "NotificationServiceUp", Service = "NotificationService" });
}
