using VpnPortal.Application.Contracts.Internal;
using VpnPortal.Application.Interfaces;
using VpnPortal.Domain.Entities;
using VpnPortal.Domain.Enums;

namespace VpnPortal.Infrastructure.Services;

public sealed class VpnAuthEventService(
    IUserRepository userRepository,
    IDeviceCredentialRepository deviceCredentialRepository,
    ITrustedIpRepository trustedIpRepository,
    IIpChangeConfirmationRepository ipChangeConfirmationRepository,
    ITokenProtector tokenProtector,
    IEmailService emailService,
    IAuditService auditService) : IVpnAuthEventService
{
    public async Task<bool> RecordAsync(VpnAuthEventCommand command, CancellationToken cancellationToken)
    {
        if (!string.Equals(command.EventType?.Trim(), "blocked_new_ip", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(command.VpnUsername) || string.IsNullOrWhiteSpace(command.SourceIp))
        {
            return false;
        }

        var credential = await deviceCredentialRepository.GetActiveByVpnUsernameAsync(command.VpnUsername, cancellationToken);
        if (credential is null)
        {
            return false;
        }

        var user = await userRepository.GetByIdAsync(credential.UserId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        var activeTrustedIp = await trustedIpRepository.GetByUserAndIpAsync(user.Id, command.SourceIp, cancellationToken);
        if (activeTrustedIp is not null && activeTrustedIp.Status == TrustedIpStatus.Active)
        {
            return true;
        }

        var pending = await ipChangeConfirmationRepository.GetPendingByUserIdAsync(user.Id, cancellationToken);
        if (pending.Any(x => x.DeviceId == credential.DeviceId && string.Equals(x.RequestedIp, command.SourceIp, StringComparison.OrdinalIgnoreCase) && x.Status == IpChangeConfirmationStatus.Pending && x.ExpiresAt > DateTimeOffset.UtcNow))
        {
            return true;
        }

        var rawToken = tokenProtector.GenerateRawToken();
        var confirmation = new IpChangeConfirmation
        {
            UserId = user.Id,
            DeviceId = credential.DeviceId,
            RequestedIp = command.SourceIp,
            TokenHash = tokenProtector.Hash(rawToken),
            Status = IpChangeConfirmationStatus.Pending,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            CreatedAt = command.OccurredAt ?? DateTimeOffset.UtcNow
        };

        confirmation = await ipChangeConfirmationRepository.AddAsync(confirmation, cancellationToken);
        var confirmationLink = $"/dashboard?confirmIpToken={rawToken}";
        await emailService.SendIpConfirmationLinkAsync(user.Email, confirmationLink, confirmation.ExpiresAt, cancellationToken);
        await auditService.WriteAsync("system", null, "vpn_new_ip_blocked", "ip_change_confirmation", confirmation.Id.ToString(), command.SourceIp, new { command.VpnUsername, command.SourceIp, command.Reason }, cancellationToken);
        return true;
    }
}
