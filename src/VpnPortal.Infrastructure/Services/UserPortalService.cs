using VpnPortal.Application.Contracts.Users;
using VpnPortal.Application.Interfaces;
using VpnPortal.Domain.Entities;
using VpnPortal.Domain.Enums;

namespace VpnPortal.Infrastructure.Services;

public sealed class UserPortalService(
    IUserRepository userRepository,
    IDeviceRepository deviceRepository,
    ITrustedIpRepository trustedIpRepository,
    IIpChangeConfirmationRepository ipChangeConfirmationRepository,
    ITokenProtector tokenProtector,
    IEmailService emailService,
    IAuditService auditService) : IUserPortalService
{
    public async Task<UserDashboardDto?> GetDashboardAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdWithRelationsAsync(userId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var devices = user.Devices
            .OrderByDescending(x => x.LastSeenAt ?? x.FirstSeenAt)
            .Select(x => new TrustedDeviceDto(
                x.Id,
                x.DeviceName,
                x.DeviceType,
                x.Platform,
                x.Status.ToString().ToLowerInvariant(),
                x.FirstSeenAt,
                x.LastSeenAt))
            .ToArray();

        var trustedIps = user.TrustedIps
            .OrderByDescending(x => x.LastSeenAt ?? x.FirstSeenAt)
            .Select(x => new TrustedIpDto(
                x.Id,
                x.IpAddress,
                x.Status.ToString().ToLowerInvariant(),
                x.FirstSeenAt,
                x.LastSeenAt,
                x.ApprovedAt))
            .ToArray();

        var confirmations = await ipChangeConfirmationRepository.GetPendingByUserIdAsync(userId, cancellationToken);
        var pendingConfirmations = confirmations
            .Where(x => x.Status == IpChangeConfirmationStatus.Pending)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new IpChangeConfirmationDto(
                x.Id,
                x.RequestedIp,
                x.Status.ToString().ToLowerInvariant(),
                x.ExpiresAt,
                x.CreatedAt,
                null))
            .ToArray();

        var sessions = user.Sessions
            .OrderByDescending(x => x.StartedAt)
            .Select(x => new VpnSessionDto(
                x.Id,
                x.SourceIp,
                x.AssignedVpnIp,
                x.Device?.DeviceName,
                x.StartedAt,
                x.LastSeenAt,
                x.Active,
                x.Authorized))
            .ToArray();

        return new UserDashboardDto(user.Id, user.Email, user.Username, user.Active, user.MaxDevices, devices, trustedIps, pendingConfirmations, sessions);
    }

    public Task<bool> RevokeDeviceAsync(int userId, int deviceId, CancellationToken cancellationToken)
    {
        return RevokeAndAuditAsync(userId, deviceId, cancellationToken);
    }

    public async Task<IpConfirmationRequestResultDto?> RequestIpConfirmationAsync(int userId, RequestIpConfirmationCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdWithRelationsAsync(userId, cancellationToken);
        if (user is null || string.IsNullOrWhiteSpace(command.RequestedIp))
        {
            return null;
        }

        if (command.DeviceId is int deviceId)
        {
            var device = await deviceRepository.GetByIdAsync(deviceId, cancellationToken);
            if (device is null || device.UserId != userId)
            {
                return null;
            }
        }

        var existing = await trustedIpRepository.GetByUserAndIpAsync(userId, command.RequestedIp, cancellationToken);
        if (existing is not null && existing.Status == TrustedIpStatus.Active)
        {
            return new IpConfirmationRequestResultDto(existing.Id, existing.IpAddress, existing.ApprovedAt ?? DateTimeOffset.UtcNow, $"/confirm-ip/already-approved", "IP address is already approved.");
        }

        var rawToken = tokenProtector.GenerateRawToken();
        var confirmation = new IpChangeConfirmation
        {
            UserId = userId,
            DeviceId = command.DeviceId,
            RequestedIp = command.RequestedIp,
            TokenHash = tokenProtector.Hash(rawToken),
            Status = IpChangeConfirmationStatus.Pending,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            CreatedAt = DateTimeOffset.UtcNow
        };

        confirmation = await ipChangeConfirmationRepository.AddAsync(confirmation, cancellationToken);
        var confirmationLink = $"/dashboard?confirmIpToken={rawToken}";
        await emailService.SendIpConfirmationLinkAsync(user.Email, confirmationLink, confirmation.ExpiresAt, cancellationToken);
        await auditService.WriteAsync("user", userId, "ip_confirmation_requested", "ip_change_confirmation", confirmation.Id.ToString(), null, new { confirmation.RequestedIp }, cancellationToken);

        return new IpConfirmationRequestResultDto(confirmation.Id, confirmation.RequestedIp, confirmation.ExpiresAt, confirmationLink, "Confirmation link created and queued for delivery.");
    }

    public async Task<bool> ConfirmIpChangeAsync(int userId, string token, CancellationToken cancellationToken)
    {
        var tokenHash = tokenProtector.Hash(token);
        var confirmation = await ipChangeConfirmationRepository.GetByHashAsync(tokenHash, cancellationToken);
        if (confirmation is null || confirmation.UserId != userId || confirmation.Status != IpChangeConfirmationStatus.Pending || confirmation.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return false;
        }

        var trustedIp = await trustedIpRepository.GetByUserAndIpAsync(userId, confirmation.RequestedIp, cancellationToken);
        if (trustedIp is null)
        {
            trustedIp = new TrustedIp
            {
                UserId = userId,
                DeviceId = confirmation.DeviceId,
                IpAddress = confirmation.RequestedIp,
                Status = TrustedIpStatus.Active,
                FirstSeenAt = DateTimeOffset.UtcNow,
                LastSeenAt = DateTimeOffset.UtcNow,
                ApprovedAt = DateTimeOffset.UtcNow
            };

            await trustedIpRepository.AddAsync(trustedIp, cancellationToken);
        }
        else
        {
            trustedIp.Status = TrustedIpStatus.Active;
            trustedIp.LastSeenAt = DateTimeOffset.UtcNow;
            trustedIp.ApprovedAt = DateTimeOffset.UtcNow;
            trustedIp.DeviceId = confirmation.DeviceId;
            await trustedIpRepository.UpdateAsync(trustedIp, cancellationToken);
        }

        confirmation.Status = IpChangeConfirmationStatus.Confirmed;
        confirmation.ConfirmedAt = DateTimeOffset.UtcNow;
        await ipChangeConfirmationRepository.UpdateAsync(confirmation, cancellationToken);
        await auditService.WriteAsync("user", userId, "ip_confirmed", "trusted_ip", confirmation.RequestedIp, null, new { confirmation.RequestedIp }, cancellationToken);
        return true;
    }

    private async Task<bool> RevokeAndAuditAsync(int userId, int deviceId, CancellationToken cancellationToken)
    {
        var revoked = await deviceRepository.RevokeAsync(userId, deviceId, cancellationToken);
        if (revoked)
        {
            await auditService.WriteAsync("user", userId, "device_revoked", "trusted_device", deviceId.ToString(), null, new { deviceId }, cancellationToken);
        }

        return revoked;
    }
}
