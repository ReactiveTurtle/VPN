using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using VpnPortal.Api.Contracts;
using VpnPortal.Application.Contracts.Requests;
using VpnPortal.Application.Interfaces;

namespace VpnPortal.Api.Controllers;

[ApiController]
[Authorize(Roles = "SuperAdmin")]
[Route("api/admin/requests")]
public sealed class AdminRequestsController(IRequestService requestService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<VpnRequestDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<VpnRequestDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await requestService.GetAllAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("{requestId:int}/approve")]
    [ProducesResponseType<VpnRequestDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Approve(int requestId, [FromBody] AdminProcessRequestInput? input, CancellationToken cancellationToken)
    {
        var result = await requestService.ApproveAsync(requestId, input?.Comment, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{requestId:int}/reject")]
    [ProducesResponseType<VpnRequestDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reject(int requestId, [FromBody] AdminProcessRequestInput? input, CancellationToken cancellationToken)
    {
        var result = await requestService.RejectAsync(requestId, input?.Comment, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
