using System.Diagnostics;
using Microsoft.Extensions.Options;
using VpnPortal.Application.Interfaces;
using VpnPortal.Domain.Entities;
using VpnPortal.Infrastructure.Options;

namespace VpnPortal.Infrastructure.Services;

public sealed class VpnRuntimeControlService(IOptions<VpnRuntimeOptions> options) : IVpnRuntimeControlService
{
    public async Task<bool> RequestDisconnectAsync(VpnSession session, CancellationToken cancellationToken)
    {
        var scriptPath = options.Value.DisconnectScriptPath?.Trim();
        if (string.IsNullOrWhiteSpace(scriptPath) || !File.Exists(scriptPath))
        {
            return false;
        }

        var processStartInfo = new ProcessStartInfo
        {
            FileName = scriptPath,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        processStartInfo.ArgumentList.Add(session.SessionId ?? string.Empty);
        processStartInfo.ArgumentList.Add(session.User?.Username ?? string.Empty);
        processStartInfo.ArgumentList.Add(session.SourceIp);
        processStartInfo.ArgumentList.Add(session.AssignedVpnIp ?? string.Empty);
        processStartInfo.ArgumentList.Add(session.NasIdentifier ?? string.Empty);

        using var process = new Process { StartInfo = processStartInfo };
        process.Start();
        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode == 0;
    }
}
