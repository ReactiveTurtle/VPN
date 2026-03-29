using Microsoft.EntityFrameworkCore;
using VpnPortal.Application.Interfaces;
using VpnPortal.Domain.Entities;
using VpnPortal.Infrastructure.Persistence.Ef.Entities;
using VpnPortal.Infrastructure.Persistence.Ef.Mappers;

namespace VpnPortal.Infrastructure.Persistence.Ef.Repositories;

public sealed class EfDeviceCredentialRepository(VpnPortalDbContext dbContext) : IDeviceCredentialRepository
{
    public async Task<VpnDeviceCredential?> GetActiveByVpnUsernameAsync(string vpnUsername, CancellationToken cancellationToken)
    {
        var lowered = vpnUsername.Trim().ToLowerInvariant();
        var entity = await dbContext.VpnDeviceCredentials
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.VpnUsername.ToLower() == lowered && x.Status == "active", cancellationToken);

        return entity?.ToDomain();
    }

    public async Task<VpnDeviceCredential?> GetActiveByDeviceIdAsync(int deviceId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.VpnDeviceCredentials
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.DeviceId == deviceId && x.Status == "active", cancellationToken);

        return entity?.ToDomain();
    }

    public async Task<VpnDeviceCredential> AddAsync(VpnDeviceCredential credential, CancellationToken cancellationToken)
    {
        var entity = new VpnDeviceCredentialEntity();
        entity.ApplyFromDomain(credential);
        await dbContext.VpnDeviceCredentials.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToDomain();
    }

    public async Task UpdateAsync(VpnDeviceCredential credential, CancellationToken cancellationToken)
    {
        var entity = await dbContext.VpnDeviceCredentials.SingleAsync(x => x.Id == credential.Id, cancellationToken);
        entity.ApplyFromDomain(credential);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> RevokeActiveByDeviceIdAsync(int userId, int deviceId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.VpnDeviceCredentials.SingleOrDefaultAsync(x => x.UserId == userId && x.DeviceId == deviceId && x.Status == "active", cancellationToken);
        if (entity is null)
        {
            return false;
        }

        entity.Status = "revoked";
        entity.RevokedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
