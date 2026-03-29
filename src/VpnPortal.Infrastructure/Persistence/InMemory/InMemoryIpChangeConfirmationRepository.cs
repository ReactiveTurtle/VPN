using VpnPortal.Application.Interfaces;
using VpnPortal.Domain.Entities;

namespace VpnPortal.Infrastructure.Persistence.InMemory;

public sealed class InMemoryIpChangeConfirmationRepository(InMemoryPortalStore store) : IIpChangeConfirmationRepository
{
    public Task<IReadOnlyCollection<IpChangeConfirmation>> GetPendingByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        var items = store.IpChangeConfirmations
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(Clone)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<IpChangeConfirmation>>(items);
    }

    public Task<IpChangeConfirmation> AddAsync(IpChangeConfirmation confirmation, CancellationToken cancellationToken)
    {
        var copy = Clone(confirmation);
        copy.Id = store.AllocateIpConfirmationId();
        store.IpChangeConfirmations.Add(copy);
        return Task.FromResult(Clone(copy));
    }

    public Task<IpChangeConfirmation?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        var item = store.IpChangeConfirmations.FirstOrDefault(x => x.TokenHash == tokenHash);
        return Task.FromResult(item is null ? null : Clone(item));
    }

    public Task UpdateAsync(IpChangeConfirmation confirmation, CancellationToken cancellationToken)
    {
        var current = store.IpChangeConfirmations.First(x => x.Id == confirmation.Id);
        current.DeviceId = confirmation.DeviceId;
        current.RequestedIp = confirmation.RequestedIp;
        current.TokenHash = confirmation.TokenHash;
        current.Status = confirmation.Status;
        current.ExpiresAt = confirmation.ExpiresAt;
        current.CreatedAt = confirmation.CreatedAt;
        current.ConfirmedAt = confirmation.ConfirmedAt;
        return Task.CompletedTask;
    }

    private static IpChangeConfirmation Clone(IpChangeConfirmation source) => new()
    {
        Id = source.Id,
        UserId = source.UserId,
        DeviceId = source.DeviceId,
        RequestedIp = source.RequestedIp,
        TokenHash = source.TokenHash,
        Status = source.Status,
        ExpiresAt = source.ExpiresAt,
        CreatedAt = source.CreatedAt,
        ConfirmedAt = source.ConfirmedAt
    };
}
