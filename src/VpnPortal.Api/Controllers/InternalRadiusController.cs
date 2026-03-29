using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VpnPortal.Application.Contracts.Internal;
using VpnPortal.Application.Interfaces;
using VpnPortal.Infrastructure.Options;

namespace VpnPortal.Api.Controllers;

[ApiController]
[Route("api/internal/radius")]
public sealed class InternalRadiusController(
    IVpnAccountingService vpnAccountingService,
    IOptions<InternalApiOptions> internalApiOptions) : ControllerBase
{
    [IgnoreAntiforgeryToken]
    [HttpPost("accounting-events")]
    public async Task<IActionResult> RecordAccountingEvent([FromBody] VpnAccountingEventCommand command, CancellationToken cancellationToken)
    {
        if (!IsAuthorizedRequest())
        {
            return Unauthorized();
        }

        var recorded = await vpnAccountingService.RecordAsync(command, cancellationToken);
        return recorded ? Ok() : BadRequest();
    }

    private bool IsAuthorizedRequest()
    {
        var configuredSecret = internalApiOptions.Value.SharedSecret;
        if (string.IsNullOrWhiteSpace(configuredSecret))
        {
            return false;
        }

        if (!Request.Headers.TryGetValue("X-Internal-Api-Key", out var providedSecret))
        {
            return false;
        }

        return string.Equals(providedSecret.ToString(), configuredSecret, StringComparison.Ordinal);
    }
}
