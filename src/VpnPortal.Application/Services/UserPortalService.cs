using VpnPortal.Application.Contracts.Users;
using VpnPortal.Application.Interfaces;
using VpnPortal.Domain.Entities;
using VpnPortal.Domain.Enums;

namespace VpnPortal.Application.Services;

public sealed class UserPortalService(
    IUserRepository userRepository,
    IDeviceRepository deviceRepository,
    IDeviceCredentialRepository deviceCredentialRepository,
    ITrustedIpRepository trustedIpRepository,
    ITokenProtector tokenProtector,
    IVpnPasswordMaterialService vpnPasswordMaterialService,
    IVpnOnboardingInstructionService vpnOnboardingInstructionService,
    IAuditService auditService) : IUserPortalService
{
    public async Task<UserDashboardDto?> GetDashboardAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdWithRelationsAsync(userId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var boundIpsByDeviceId = user.TrustedIps
            .Where(x => x.Status == TrustedIpStatus.Active && x.DeviceId is not null)
            .GroupBy(x => x.DeviceId!.Value)
            .ToDictionary(
                x => x.Key,
                x => x.OrderByDescending(y => y.LastSeenAt ?? y.ApprovedAt ?? y.FirstSeenAt).First());

        var devices = user.Devices
            .OrderByDescending(x => x.LastSeenAt ?? x.FirstSeenAt)
            .Select(x =>
            {
                boundIpsByDeviceId.TryGetValue(x.Id, out var boundIp);

                return new TrustedDeviceDto(
                    x.Id,
                    x.DeviceName,
                    x.Status.ToString().ToLowerInvariant(),
                    x.ActiveCredential?.VpnUsername,
                    x.ActiveCredential?.Status.ToString().ToLowerInvariant(),
                    x.ActiveCredential?.RotatedAt,
                    boundIp?.IpAddress,
                    boundIp?.LastSeenAt,
                    x.FirstSeenAt,
                    x.LastSeenAt);
            })
            .ToArray();

        var platformGuides = vpnOnboardingInstructionService.CreateCatalog();

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

        return new UserDashboardDto(user.Id, user.Email, user.Active, user.MaxDevices, platformGuides, devices, sessions);
    }

    public async Task<IssuedVpnDeviceCredentialDto?> IssueDeviceCredentialAsync(int userId, IssueVpnDeviceCredentialCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdWithRelationsAsync(userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var deviceName = command.DeviceName?.Trim();
        if (string.IsNullOrWhiteSpace(deviceName))
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
            DeviceType = "device",
            Platform = "manual",
            Status = DeviceStatus.Active,
            FirstSeenAt = now,
            LastSeenAt = now
        }, cancellationToken);

        var vpnPassword = tokenProtector.GenerateRawToken(24);
        var passwordMaterial = vpnPasswordMaterialService.Create(vpnPassword);
        var credential = await deviceCredentialRepository.AddAsync(new VpnDeviceCredential
        {
            UserId = userId,
            DeviceId = device.Id,
            VpnUsername = BuildVpnUsername(user.Username, device.Id),
            PasswordHash = passwordMaterial.PasswordHash,
            RadiusNtHash = passwordMaterial.RadiusNtHash,
            Status = VpnDeviceCredentialStatus.Active,
            CreatedAt = now
        }, cancellationToken);

        await auditService.WriteAsync("user", userId, "device_credential_issued", "vpn_device_credential", credential.Id.ToString(), null, new { device.Id, credential.VpnUsername }, cancellationToken);
        return new IssuedVpnDeviceCredentialDto(device.Id, device.DeviceName, credential.VpnUsername, vpnPassword, vpnOnboardingInstructionService.Create(device.Platform, credential.VpnUsername), "Доступ для устройства создан. Сохраните пароль сейчас: повторно он показан не будет.");
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
        var passwordMaterial = vpnPasswordMaterialService.Create(vpnPassword);
        credential.Rotate(passwordMaterial.PasswordHash, passwordMaterial.RadiusNtHash, DateTimeOffset.UtcNow);
        await deviceCredentialRepository.UpdateAsync(credential, cancellationToken);
        await auditService.WriteAsync("user", userId, "device_credential_rotated", "vpn_device_credential", credential.Id.ToString(), null, new { deviceId, credential.VpnUsername }, cancellationToken);

        return new IssuedVpnDeviceCredentialDto(device.Id, device.DeviceName, credential.VpnUsername, vpnPassword, vpnOnboardingInstructionService.Create(device.Platform, credential.VpnUsername), "Пароль для устройства обновлен. Сохраните новый пароль сейчас: повторно он показан не будет.");
    }

    public Task<bool> RevokeDeviceAsync(int userId, int deviceId, CancellationToken cancellationToken)
    {
        return RevokeAndAuditAsync(userId, deviceId, cancellationToken);
    }

    public async Task<bool> UnbindDeviceIpAsync(int userId, int deviceId, CancellationToken cancellationToken)
    {
        var device = await deviceRepository.GetByIdAsync(deviceId, cancellationToken);
        if (device is null || device.UserId != userId)
        {
            return false;
        }

        var trustedIp = await trustedIpRepository.GetActiveByDeviceIdAsync(deviceId, cancellationToken);
        if (trustedIp is null)
        {
            return false;
        }

        trustedIp.Revoke(DateTimeOffset.UtcNow);
        await trustedIpRepository.UpdateAsync(trustedIp, cancellationToken);
        await auditService.WriteAsync("user", userId, "device_ip_unbound", "trusted_ip", trustedIp.Id.ToString(), null, new { deviceId, trustedIp.IpAddress }, cancellationToken);
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
