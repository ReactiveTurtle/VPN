using VpnPortal.Application.Contracts.Internal;
using VpnPortal.Application.Interfaces;
using VpnPortal.Domain.Entities;

namespace VpnPortal.Application.Services;

public sealed class VpnAccountingService(
    IDeviceCredentialRepository deviceCredentialRepository,
    ISessionRepository sessionRepository,
    IAuditService auditService) : IVpnAccountingService
{
    public async Task<bool> RecordAsync(VpnAccountingEventCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.EventType) || string.IsNullOrWhiteSpace(command.VpnUsername) || string.IsNullOrWhiteSpace(command.SessionId) || string.IsNullOrWhiteSpace(command.SourceIp))
        {
            return false;
        }

        var credential = await deviceCredentialRepository.GetActiveByVpnUsernameAsync(command.VpnUsername, cancellationToken);
        if (credential is null)
        {
            return false;
        }

        var occurredAt = command.OccurredAt ?? DateTimeOffset.UtcNow;
        var eventType = command.EventType.Trim().ToLowerInvariant();
        if (eventType is "start" or "interim")
        {
            await sessionRepository.RecordAuthorizedAsync(new VpnSession
            {
                UserId = credential.UserId,
                DeviceId = credential.DeviceId,
                SourceIp = command.SourceIp,
                AssignedVpnIp = command.AssignedVpnIp,
                NasIdentifier = command.NasIdentifier,
                SessionId = command.SessionId,
                StartedAt = occurredAt,
                LastSeenAt = occurredAt,
                Active = true,
                Authorized = true
            }, cancellationToken);

            await auditService.WriteAsync("system", null, "vpn_accounting_recorded", "vpn_session", command.SessionId, null, new { command.VpnUsername, eventType }, cancellationToken);
            return true;
        }

        if (eventType == "stop")
        {
            var closed = await sessionRepository.CloseBySessionIdAsync(credential.UserId, command.SessionId, occurredAt, command.TerminationReason, cancellationToken);
            if (closed)
            {
                await auditService.WriteAsync("system", null, "vpn_session_closed", "vpn_session", command.SessionId, null, new { command.VpnUsername, eventType }, cancellationToken);
            }

            return closed;
        }

        return false;
    }
}
