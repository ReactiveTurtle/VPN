using System.Net;
using Microsoft.EntityFrameworkCore;
using VpnPortal.Application.Interfaces;
using VpnPortal.Domain.Entities;
using VpnPortal.Infrastructure.Persistence.Ef.Entities;
using VpnPortal.Infrastructure.Persistence.Ef.Mappers;

namespace VpnPortal.Infrastructure.Persistence.Ef.Repositories;

public sealed class EfTrustedIpRepository(VpnPortalDbContext dbContext) : ITrustedIpRepository
{
    public async Task<IReadOnlyCollection<TrustedIp>> GetByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        var entities = await dbContext.TrustedIps
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.LastSeenAt ?? x.FirstSeenAt)
            .ToListAsync(cancellationToken);

        return entities.Select(x => x.ToDomain()).ToArray();
    }

    public async Task<TrustedIp?> GetByUserAndIpAsync(int userId, string ipAddress, CancellationToken cancellationToken)
    {
        var parsedIp = IPAddress.Parse(ipAddress);
        var entity = await dbContext.TrustedIps
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == userId && x.IpAddress == parsedIp, cancellationToken);

        return entity?.ToDomain();
    }

    public async Task<TrustedIp> AddAsync(TrustedIp trustedIp, CancellationToken cancellationToken)
    {
        var entity = new TrustedIpEntity();
        entity.ApplyFromDomain(trustedIp);
        await dbContext.TrustedIps.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToDomain();
    }

    public async Task UpdateAsync(TrustedIp trustedIp, CancellationToken cancellationToken)
    {
        var entity = await dbContext.TrustedIps.SingleAsync(x => x.Id == trustedIp.Id, cancellationToken);
        entity.ApplyFromDomain(trustedIp);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
