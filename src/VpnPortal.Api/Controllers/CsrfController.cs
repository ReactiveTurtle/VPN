using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace VpnPortal.Api.Controllers;

[ApiController]
[Route("api/csrf")]
public sealed class CsrfController(IAntiforgery antiforgery) : ControllerBase
{
    [HttpGet("token")]
    public IActionResult GetToken()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { token = tokens.RequestToken });
    }
}
