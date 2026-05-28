using Microsoft.AspNetCore.Mvc;

namespace SimPle.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "healthy", utc = DateTime.UtcNow, version = "0.1.0" });
}
