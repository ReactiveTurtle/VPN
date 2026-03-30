using Microsoft.AspNetCore.Mvc;
using VpnPortal.Application.Contracts.System;
using VpnPortal.Application.Interfaces;

namespace VpnPortal.Api.Controllers;

[ApiController]
[Route("api/system")]
public sealed class SystemController(ISystemStatusService systemStatusService, IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet("status")]
    [ProducesResponseType<AppStatusDto>(StatusCodes.Status200OK)]
    public ActionResult<AppStatusDto> GetStatus()
    {
        var result = new AppStatusDto(
            "VpnPortal.Api",
            typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0",
            systemStatusService.IsDatabaseConfigured,
            environment.IsDevelopment());

        return Ok(result);
    }

    [HttpGet("database")]
    [ProducesResponseType<DatabaseStatusDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DatabaseStatusDto>> GetDatabaseStatus(CancellationToken cancellationToken)
    {
        var result = await systemStatusService.GetDatabaseStatusAsync(cancellationToken);
        return Ok(result);
    }
}
