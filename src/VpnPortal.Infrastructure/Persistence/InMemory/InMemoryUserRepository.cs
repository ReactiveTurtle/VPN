using VpnPortal.Application.Interfaces;
using VpnPortal.Domain.Entities;

namespace VpnPortal.Infrastructure.Persistence.InMemory;

public sealed class InMemoryUserRepository(InMemoryPortalStore store) : IUserRepository
{
    public Task<IReadOnlyCollection<VpnUser>> GetAllAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<VpnUser>>(store.Users.Select(Clone).ToArray());
    }

    public Task<VpnUser?> GetByIdAsync(int userId, CancellationToken cancellationToken)
    {
        var user = store.Users.Where(x => x.Id == userId).Select(Clone).FirstOrDefault();
        return Task.FromResult(user);
    }

    public Task<VpnUser?> GetByIdWithRelationsAsync(int userId, CancellationToken cancellationToken)
    {
        var user = store.Users.Where(x => x.Id == userId).Select(Clone).FirstOrDefault();
        return Task.FromResult(user);
    }

    public Task<VpnUser?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var user = store.Users.Where(x => x.Email == email).Select(Clone).FirstOrDefault();
        return Task.FromResult(user);
    }

    public Task<VpnUser?> GetByUsernameOrEmailAsync(string login, CancellationToken cancellationToken)
    {
        var normalized = login.Trim().ToLowerInvariant();
        var user = store.Users
            .Where(x => x.Email.ToLowerInvariant() == normalized || x.Username.ToLowerInvariant() == normalized)
            .Select(Clone)
            .FirstOrDefault();
        return Task.FromResult(user);
    }

    public Task<VpnUser> AddAsync(VpnUser user, CancellationToken cancellationToken)
    {
        var copy = Clone(user);
        copy.Id = store.AllocateUserId();
        store.Users.Add(copy);
        return Task.FromResult(Clone(copy));
    }

    public Task UpdateAsync(VpnUser user, CancellationToken cancellationToken)
    {
        var current = store.Users.First(x => x.Id == user.Id);
        current.Email = user.Email;
        current.Username = user.Username;
        current.PasswordHash = user.PasswordHash;
        current.MaxDevices = user.MaxDevices;
        current.Active = user.Active;
        current.EmailConfirmed = user.EmailConfirmed;
        current.CreatedAt = user.CreatedAt;
        current.LastLoginAt = user.LastLoginAt;
        return Task.CompletedTask;
    }

    private VpnUser Clone(VpnUser source)
    {
        return new VpnUser
        {
            Id = source.Id,
            Email = source.Email,
            Username = source.Username,
            PasswordHash = source.PasswordHash,
            MaxDevices = source.MaxDevices,
            Active = source.Active,
            EmailConfirmed = source.EmailConfirmed,
            CreatedAt = source.CreatedAt,
            LastLoginAt = source.LastLoginAt,
            TrustedIps = source.TrustedIps.Select(ip => new TrustedIp
            {
                Id = ip.Id,
                UserId = ip.UserId,
                DeviceId = ip.DeviceId,
                IpAddress = ip.IpAddress,
                Status = ip.Status,
                FirstSeenAt = ip.FirstSeenAt,
                LastSeenAt = ip.LastSeenAt,
                ApprovedAt = ip.ApprovedAt,
                RevokedAt = ip.RevokedAt
            }).ToArray(),
            Devices = source.Devices.Select(device => new TrustedDevice
            {
                Id = device.Id,
                UserId = device.UserId,
                DeviceUuid = device.DeviceUuid,
                DeviceName = device.DeviceName,
                DeviceType = device.DeviceType,
                Platform = device.Platform,
                Status = device.Status,
                FirstSeenAt = device.FirstSeenAt,
                LastSeenAt = device.LastSeenAt,
                ActiveCredential = store.DeviceCredentials
                    .Where(c => c.DeviceId == device.Id && c.Status == Domain.Enums.VpnDeviceCredentialStatus.Active)
                    .Select(c => new VpnDeviceCredential
                    {
                        Id = c.Id,
                        UserId = c.UserId,
                        DeviceId = c.DeviceId,
                        VpnUsername = c.VpnUsername,
                        PasswordHash = c.PasswordHash,
                        Status = c.Status,
                        CreatedAt = c.CreatedAt,
                        RotatedAt = c.RotatedAt,
                        RevokedAt = c.RevokedAt,
                        LastUsedAt = c.LastUsedAt
                    })
                    .FirstOrDefault()
            }).ToArray(),
            Sessions = source.Sessions.Select(session => new VpnSession
            {
                Id = session.Id,
                UserId = session.UserId,
                DeviceId = session.DeviceId,
                SourceIp = session.SourceIp,
                AssignedVpnIp = session.AssignedVpnIp,
                SessionId = session.SessionId,
                StartedAt = session.StartedAt,
                LastSeenAt = session.LastSeenAt,
                EndedAt = session.EndedAt,
                Active = session.Active,
                Authorized = session.Authorized,
                Device = source.Devices.FirstOrDefault(d => d.Id == session.DeviceId) is { } device
                    ? new TrustedDevice
                    {
                        Id = device.Id,
                        UserId = device.UserId,
                        DeviceUuid = device.DeviceUuid,
                        DeviceName = device.DeviceName,
                        DeviceType = device.DeviceType,
                        Platform = device.Platform,
                        Status = device.Status,
                        FirstSeenAt = device.FirstSeenAt,
                        LastSeenAt = device.LastSeenAt
                    }
                    : null
            }).ToArray()
        };
    }
}
