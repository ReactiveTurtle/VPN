using VpnPortal.Application.Interfaces;
using VpnPortal.Domain.Entities;
using VpnPortal.Domain.Enums;

namespace VpnPortal.Infrastructure.Persistence.InMemory;

public sealed class InMemoryDeviceCredentialRepository(InMemoryPortalStore store) : IDeviceCredentialRepository
{
    public Task<VpnDeviceCredential?> GetActiveByDeviceIdAsync(int deviceId, CancellationToken cancellationToken)
    {
        var credential = store.DeviceCredentials
            .FirstOrDefault(x => x.DeviceId == deviceId && x.Status == VpnDeviceCredentialStatus.Active);

        return Task.FromResult(credential is null ? null : Clone(credential));
    }

    public Task<VpnDeviceCredential> AddAsync(VpnDeviceCredential credential, CancellationToken cancellationToken)
    {
        var copy = Clone(credential);
        copy.Id = store.AllocateDeviceCredentialId();
        store.DeviceCredentials.Add(copy);
        return Task.FromResult(Clone(copy));
    }

    public Task UpdateAsync(VpnDeviceCredential credential, CancellationToken cancellationToken)
    {
        var current = store.DeviceCredentials.First(x => x.Id == credential.Id);
        current.PasswordHash = credential.PasswordHash;
        current.Status = credential.Status;
        current.RotatedAt = credential.RotatedAt;
        current.RevokedAt = credential.RevokedAt;
        current.LastUsedAt = credential.LastUsedAt;
        return Task.CompletedTask;
    }

    public Task<bool> RevokeActiveByDeviceIdAsync(int userId, int deviceId, CancellationToken cancellationToken)
    {
        var credential = store.DeviceCredentials
            .FirstOrDefault(x => x.UserId == userId && x.DeviceId == deviceId && x.Status == VpnDeviceCredentialStatus.Active);

        if (credential is null)
        {
            return Task.FromResult(false);
        }

        credential.Status = VpnDeviceCredentialStatus.Revoked;
        credential.RevokedAt = DateTimeOffset.UtcNow;
        return Task.FromResult(true);
    }

    private static VpnDeviceCredential Clone(VpnDeviceCredential source)
    {
        return new VpnDeviceCredential
        {
            Id = source.Id,
            UserId = source.UserId,
            DeviceId = source.DeviceId,
            VpnUsername = source.VpnUsername,
            PasswordHash = source.PasswordHash,
            Status = source.Status,
            CreatedAt = source.CreatedAt,
            RotatedAt = source.RotatedAt,
            RevokedAt = source.RevokedAt,
            LastUsedAt = source.LastUsedAt
        };
    }
}
