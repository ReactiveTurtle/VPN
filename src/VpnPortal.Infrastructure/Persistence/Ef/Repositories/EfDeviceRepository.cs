using Microsoft.EntityFrameworkCore;
using VpnPortal.Application.Interfaces;
using VpnPortal.Domain.Entities;
using VpnPortal.Domain.Enums;
using VpnPortal.Infrastructure.Persistence.Ef.Entities;
using VpnPortal.Infrastructure.Persistence.Ef.Mappers;

namespace VpnPortal.Infrastructure.Persistence.Ef.Repositories;

public sealed class EfDeviceRepository(VpnPortalDbContext dbContext) : IDeviceRepository
{
    public async Task<TrustedDevice?> GetByIdAsync(int deviceId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.TrustedDevices
            .AsNoTracking()
            .Include(x => x.Credential)
            .SingleOrDefaultAsync(x => x.Id == deviceId, cancellationToken);

        return entity?.ToDomain();
    }

    public async Task<TrustedDevice> AddAsync(TrustedDevice device, CancellationToken cancellationToken)
    {
        var entity = new TrustedDeviceEntity();
        entity.ApplyFromDomain(device);
        if (device.Status == DeviceStatus.Active)
        {
            entity.ApprovedAt = device.FirstSeenAt;
        }

        await dbContext.TrustedDevices.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToDomain();
    }

    public async Task<bool> RevokeAsync(int userId, int deviceId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.TrustedDevices.SingleOrDefaultAsync(x => x.Id == deviceId && x.UserId == userId, cancellationToken);
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
