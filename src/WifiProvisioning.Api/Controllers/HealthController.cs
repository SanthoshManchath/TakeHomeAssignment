using Microsoft.AspNetCore.Mvc;

namespace WifiProvisioning.Api.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get() =>
        Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow });
}