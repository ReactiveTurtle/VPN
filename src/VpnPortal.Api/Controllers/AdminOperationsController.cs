using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VpnPortal.Api.Contracts;
using VpnPortal.Application.Contracts.Admin;
using VpnPortal.Application.Interfaces;

namespace VpnPortal.Api.Controllers;

[ApiController]
[Authorize(Roles = "SuperAdmin")]
[Route("api/admin")]
public sealed class AdminOperationsController(IAdminOperationsService adminOperationsService) : ControllerBase
{
    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyCollection<AdminUserDto>>> GetUsers(CancellationToken cancellationToken)
    {
        return Ok(await adminOperationsService.GetUsersAsync(cancellationToken));
    }

    [HttpPatch("users/{userId:int}")]
    public async Task<IActionResult> UpdateUser(int userId, [FromBody] AdminUpdateUserInput input, CancellationToken cancellationToken)
    {
        var actorId = GetActorId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await adminOperationsService.UpdateUserAsync(userId, input.MaxDevices, actorId, ipAddress, cancellationToken);
        return result is null ? BadRequest() : Ok(result);
    }

    [HttpPost("users/{userId:int}/status")]
    public async Task<IActionResult> SetUserStatus(int userId, [FromBody] AdminSetUserActiveInput input, CancellationToken cancellationToken)
    {
        var actorId = GetActorId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await adminOperationsService.SetUserActiveAsync(userId, input.Active, actorId, ipAddress, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("sessions")]
    public async Task<ActionResult<IReadOnlyCollection<AdminSessionDto>>> GetSessions(CancellationToken cancellationToken)
    {
        return Ok(await adminOperationsService.GetSessionsAsync(cancellationToken));
    }

    [HttpPost("sessions/{sessionId:int}/disconnect")]
    public async Task<IActionResult> DisconnectSession(int sessionId, CancellationToken cancellationToken)
    {
        var actorId = GetActorId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await adminOperationsService.DisconnectSessionAsync(sessionId, actorId, ipAddress, cancellationToken);
        return result ? Ok() : NotFound();
    }

    [HttpGet("audit")]
    public async Task<ActionResult<IReadOnlyCollection<AuditLogDto>>> GetAudit(CancellationToken cancellationToken)
    {
        return Ok(await adminOperationsService.GetAuditLogAsync(cancellationToken));
    }

    private long? GetActorId()
    {
        return long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed) ? parsed : null;
    }
}
