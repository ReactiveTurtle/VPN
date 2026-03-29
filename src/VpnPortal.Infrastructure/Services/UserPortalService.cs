using VpnPortal.Application.Contracts.Users;
using VpnPortal.Application.Interfaces;
using VpnPortal.Domain.Entities;
using VpnPortal.Domain.Enums;

namespace VpnPortal.Infrastructure.Services;

public sealed class UserPortalService(
    IUserRepository userRepository,
    IDeviceRepository deviceRepository,
    IDeviceCredentialRepository deviceCredentialRepository,
    ITrustedIpRepository trustedIpRepository,
    IIpChangeConfirmationRepository ipChangeConfirmationRepository,
    ITokenProtector tokenProtector,
    IPasswordHasher passwordHasher,
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
                x.ActiveCredential?.VpnUsername,
                x.ActiveCredential?.Status.ToString().ToLowerInvariant(),
                x.ActiveCredential?.RotatedAt,
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

    public async Task<IssuedVpnDeviceCredentialDto?> IssueDeviceCredentialAsync(int userId, IssueVpnDeviceCredentialCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdWithRelationsAsync(userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var deviceName = command.DeviceName?.Trim();
        var deviceType = command.DeviceType?.Trim().ToLowerInvariant();
        var platform = command.Platform?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(deviceName) || string.IsNullOrWhiteSpace(deviceType) || string.IsNullOrWhiteSpace(platform))
        {
            return null;
        }

        var activeDeviceCount = user.Devices.Count(x => x.Status == DeviceStatus.Active);
        if (activeDeviceCount >= user.MaxDevices)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var device = await deviceRepository.AddAsync(new TrustedDevice
        {
            UserId = userId,
            DeviceUuid = $"dev-{Guid.NewGuid():N}",
            DeviceName = deviceName,
            DeviceType = deviceType,
            Platform = platform,
            Status = DeviceStatus.Active,
            FirstSeenAt = now,
            LastSeenAt = now
        }, cancellationToken);

        var vpnPassword = tokenProtector.GenerateRawToken(24);
        var credential = await deviceCredentialRepository.AddAsync(new VpnDeviceCredential
        {
            UserId = userId,
            DeviceId = device.Id,
            VpnUsername = BuildVpnUsername(user.Username, device.Id),
            PasswordHash = passwordHasher.Hash(vpnPassword),
            Status = VpnDeviceCredentialStatus.Active,
            CreatedAt = now
        }, cancellationToken);

        await auditService.WriteAsync("user", userId, "device_credential_issued", "vpn_device_credential", credential.Id.ToString(), null, new { device.Id, credential.VpnUsername }, cancellationToken);
        return new IssuedVpnDeviceCredentialDto(device.Id, device.DeviceName, credential.VpnUsername, vpnPassword, "VPN credential issued. Save this password now because it will not be shown again.");
    }

    public async Task<IssuedVpnDeviceCredentialDto?> RotateDeviceCredentialAsync(int userId, int deviceId, CancellationToken cancellationToken)
    {
        var device = await deviceRepository.GetByIdAsync(deviceId, cancellationToken);
        if (device is null || device.UserId != userId || device.Status == DeviceStatus.Revoked)
        {
            return null;
        }

        var credential = await deviceCredentialRepository.GetActiveByDeviceIdAsync(deviceId, cancellationToken);
        if (credential is null)
        {
            return null;
        }

        var vpnPassword = tokenProtector.GenerateRawToken(24);
        credential.PasswordHash = passwordHasher.Hash(vpnPassword);
        credential.RotatedAt = DateTimeOffset.UtcNow;
        await deviceCredentialRepository.UpdateAsync(credential, cancellationToken);
        await auditService.WriteAsync("user", userId, "device_credential_rotated", "vpn_device_credential", credential.Id.ToString(), null, new { deviceId, credential.VpnUsername }, cancellationToken);

        return new IssuedVpnDeviceCredentialDto(device.Id, device.DeviceName, credential.VpnUsername, vpnPassword, "VPN credential rotated. Save the new password now because it will not be shown again.");
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
        await deviceCredentialRepository.RevokeActiveByDeviceIdAsync(userId, deviceId, cancellationToken);
        var revoked = await deviceRepository.RevokeAsync(userId, deviceId, cancellationToken);
        if (revoked)
        {
            await auditService.WriteAsync("user", userId, "device_revoked", "trusted_device", deviceId.ToString(), null, new { deviceId }, cancellationToken);
        }

        return revoked;
    }

    private static string BuildVpnUsername(string username, int deviceId)
    {
        var safeUser = new string(username.Trim().ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
        safeUser = string.IsNullOrWhiteSpace(safeUser) ? "user" : safeUser;
        return $"{safeUser}.d{deviceId}";
    }
}
