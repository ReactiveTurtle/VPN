using VpnPortal.Application.Interfaces;
using VpnPortal.Domain.Entities;

namespace VpnPortal.Infrastructure.Persistence.InMemory;

public sealed class InMemoryTrustedIpRepository(InMemoryPortalStore store) : ITrustedIpRepository
{
    public Task<IReadOnlyCollection<TrustedIp>> GetByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        var items = store.Users.FirstOrDefault(x => x.Id == userId)?.TrustedIps
            .Select(Clone)
            .ToArray() ?? [];
        return Task.FromResult<IReadOnlyCollection<TrustedIp>>(items);
    }

    public Task<TrustedIp?> GetByUserAndIpAsync(int userId, string ipAddress, CancellationToken cancellationToken)
    {
        var item = store.Users.FirstOrDefault(x => x.Id == userId)?.TrustedIps
            .FirstOrDefault(x => x.IpAddress == ipAddress);
        return Task.FromResult(item is null ? null : Clone(item));
    }

    public Task<TrustedIp> AddAsync(TrustedIp trustedIp, CancellationToken cancellationToken)
    {
        var copy = Clone(trustedIp);
        copy.Id = store.AllocateTrustedIpId();
        store.Users.First(x => x.Id == copy.UserId).TrustedIps.Add(copy);
        return Task.FromResult(Clone(copy));
    }

    public Task UpdateAsync(TrustedIp trustedIp, CancellationToken cancellationToken)
    {
        var current = store.Users.First(x => x.Id == trustedIp.UserId).TrustedIps.First(x => x.Id == trustedIp.Id);
        current.DeviceId = trustedIp.DeviceId;
        current.IpAddress = trustedIp.IpAddress;
        current.Status = trustedIp.Status;
        current.FirstSeenAt = trustedIp.FirstSeenAt;
        current.LastSeenAt = trustedIp.LastSeenAt;
        current.ApprovedAt = trustedIp.ApprovedAt;
        current.RevokedAt = trustedIp.RevokedAt;
        return Task.CompletedTask;
    }

    private static TrustedIp Clone(TrustedIp source) => new()
    {
        Id = source.Id,
        UserId = source.UserId,
        DeviceId = source.DeviceId,
        IpAddress = source.IpAddress,
        Status = source.Status,
        FirstSeenAt = source.FirstSeenAt,
        LastSeenAt = source.LastSeenAt,
        ApprovedAt = source.ApprovedAt,
        RevokedAt = source.RevokedAt
    };
}
