using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VpnPortal.Application.Contracts.Users;
using VpnPortal.Application.Interfaces;

namespace VpnPortal.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/me")]
public sealed class MeController(IUserPortalService userPortalService) : ControllerBase
{
    [HttpGet("dashboard")]
    [ProducesResponseType<UserDashboardDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = User.FindFirstValue(ClaimTypes.Role);
        if (!int.TryParse(userIdClaim, out var userId) || !string.Equals(role, "User", StringComparison.Ordinal))
        {
            return Forbid();
        }

        var result = await userPortalService.GetDashboardAsync(userId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("devices/{deviceId:int}")]
    public async Task<IActionResult> RevokeDevice(int deviceId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Forbid();
        }

        var revoked = await userPortalService.RevokeDeviceAsync(userId.Value, deviceId, cancellationToken);
        return revoked ? NoContent() : NotFound();
    }

    [HttpPost("devices")]
    [ProducesResponseType<IssuedVpnDeviceCredentialDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> IssueDeviceCredential([FromBody] IssueVpnDeviceCredentialCommand command, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Forbid();
        }

        var result = await userPortalService.IssueDeviceCredentialAsync(userId.Value, command, cancellationToken);
        return result is null ? BadRequest() : Ok(result);
    }

    [HttpPost("devices/{deviceId:int}/rotate-credential")]
    [ProducesResponseType<IssuedVpnDeviceCredentialDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> RotateDeviceCredential(int deviceId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Forbid();
        }

        var result = await userPortalService.RotateDeviceCredentialAsync(userId.Value, deviceId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("ip-confirmations/request")]
    [ProducesResponseType<IpConfirmationRequestResultDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> RequestIpConfirmation([FromBody] RequestIpConfirmationCommand command, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Forbid();
        }

        var result = await userPortalService.RequestIpConfirmationAsync(userId.Value, command, cancellationToken);
        return result is null ? BadRequest() : Ok(result);
    }

    [HttpPost("ip-confirmations/{token}/confirm")]
    public async Task<IActionResult> ConfirmIp(string token, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Forbid();
        }

        var result = await userPortalService.ConfirmIpChangeAsync(userId.Value, token, cancellationToken);
        return result ? Ok() : BadRequest();
    }

    private int? GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = User.FindFirstValue(ClaimTypes.Role);
        return int.TryParse(userIdClaim, out var userId) && string.Equals(role, "User", StringComparison.Ordinal)
            ? userId
            : null;
    }
}
