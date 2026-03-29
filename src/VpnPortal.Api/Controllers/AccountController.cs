using Microsoft.AspNetCore.Mvc;
using VpnPortal.Application.Contracts.Account;
using VpnPortal.Application.Interfaces;

namespace VpnPortal.Api.Controllers;

[ApiController]
[Route("api/account")]
public sealed class AccountController(IAccountActivationService accountActivationService) : ControllerBase
{
    [HttpGet("activate")]
    [ProducesResponseType<ActivationTokenStatusDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ActivationTokenStatusDto>> GetActivationStatus([FromQuery] string token, CancellationToken cancellationToken)
    {
        var result = await accountActivationService.GetStatusAsync(token, cancellationToken);
        return Ok(result);
    }

    [IgnoreAntiforgeryToken]
    [HttpPost("activate")]
    [ProducesResponseType<ActivationCompletedDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Activate([FromBody] ActivateAccountCommand command, CancellationToken cancellationToken)
    {
        var result = await accountActivationService.ActivateAsync(command, cancellationToken);
        return result is null ? BadRequest() : Ok(result);
    }
}
