using Microsoft.EntityFrameworkCore;
using VpnPortal.Application.Interfaces;
using VpnPortal.Domain.Entities;
using VpnPortal.Infrastructure.Persistence.Ef.Entities;
using VpnPortal.Infrastructure.Persistence.Ef.Mappers;

namespace VpnPortal.Infrastructure.Persistence.Ef.Repositories;

public sealed class EfAuditLogRepository(VpnPortalDbContext dbContext) : IAuditLogRepository
{
    public async Task<AuditLogEntry> AddAsync(AuditLogEntry entry, CancellationToken cancellationToken)
    {
        var entity = new AuditLogEntity();
        entity.ApplyFromDomain(entry);
        await dbContext.AuditLogs.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToDomain();
    }

    public async Task<IReadOnlyCollection<AuditLogEntry>> GetRecentAsync(int take, CancellationToken cancellationToken)
    {
        var entities = await dbContext.AuditLogs
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        return entities.Select(x => x.ToDomain()).ToArray();
    }
}
