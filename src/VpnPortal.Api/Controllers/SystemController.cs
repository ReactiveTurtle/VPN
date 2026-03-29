using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VpnPortal.Application.Contracts.System;
using VpnPortal.Infrastructure.Options;
using VpnPortal.Infrastructure.Services;

namespace VpnPortal.Api.Controllers;

[ApiController]
[Route("api/system")]
public sealed class SystemController(IOptions<DatabaseOptions> databaseOptions, DatabaseStatusService databaseStatusService, IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet("status")]
    [ProducesResponseType<AppStatusDto>(StatusCodes.Status200OK)]
    public ActionResult<AppStatusDto> GetStatus()
    {
        var result = new AppStatusDto(
            "VpnPortal.Api",
            typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0",
            !string.IsNullOrWhiteSpace(databaseOptions.Value.ConnectionString),
            environment.IsDevelopment());

        return Ok(result);
    }

    [HttpGet("database")]
    public async Task<IActionResult> GetDatabaseStatus(CancellationToken cancellationToken)
    {
        try
        {
            var canConnect = await databaseStatusService.CanConnectAsync(cancellationToken);
            return Ok(new { configured = databaseStatusService.IsConfigured, canConnect });
        }
        catch (Exception exception)
        {
            return Ok(new { configured = databaseStatusService.IsConfigured, canConnect = false, error = exception.Message });
        }
    }
}
