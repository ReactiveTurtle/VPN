using Microsoft.AspNetCore.Mvc;
using VpnPortal.Application.Contracts.Requests;
using VpnPortal.Application.Interfaces;

namespace VpnPortal.Api.Controllers;

[ApiController]
[Route("api/requests")]
public sealed class PublicRequestsController(IRequestService requestService) : ControllerBase
{
    [IgnoreAntiforgeryToken]
    [HttpPost]
    [ProducesResponseType<VpnRequestDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Submit([FromBody] SubmitVpnRequestCommand command, CancellationToken cancellationToken)
    {
        var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await requestService.SubmitAsync(command, remoteIp, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { id = result.Id }, result);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<VpnRequestDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<VpnRequestDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await requestService.GetAllAsync(cancellationToken);
        return Ok(result);
    }
}
