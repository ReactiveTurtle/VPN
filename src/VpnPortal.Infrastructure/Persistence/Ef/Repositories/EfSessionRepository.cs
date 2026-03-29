using System.Net;
using Microsoft.EntityFrameworkCore;
using VpnPortal.Application.Interfaces;
using VpnPortal.Domain.Entities;
using VpnPortal.Infrastructure.Persistence.Ef.Entities;
using VpnPortal.Infrastructure.Persistence.Ef.Mappers;

namespace VpnPortal.Infrastructure.Persistence.Ef.Repositories;

public sealed class EfSessionRepository(VpnPortalDbContext dbContext) : ISessionRepository
{
    public async Task<VpnSession?> GetByIdAsync(int sessionId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.VpnSessions
            .AsNoTracking()
            .Include(x => x.Device)
            .Include(x => x.User)
            .SingleOrDefaultAsync(x => x.Id == sessionId, cancellationToken);

        return entity?.ToDomain();
    }

    public async Task<IReadOnlyCollection<VpnSession>> GetRecentAsync(CancellationToken cancellationToken)
    {
        var entities = await dbContext.VpnSessions
            .AsNoTracking()
            .Include(x => x.Device)
            .Include(x => x.User)
            .OrderByDescending(x => x.StartedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        return entities.Select(x => x.ToDomain()).ToArray();
    }

    public async Task RecordAuthorizedAsync(VpnSession session, CancellationToken cancellationToken)
    {
        var entity = await dbContext.VpnSessions.SingleOrDefaultAsync(x => x.UserId == session.UserId && x.SessionId == session.SessionId, cancellationToken);
        if (entity is null)
        {
            entity = new VpnSessionEntity();
            entity.ApplyFromDomain(session);
            await dbContext.VpnSessions.AddAsync(entity, cancellationToken);
        }
        else
        {
            entity.SourceIp = IPAddress.Parse(session.SourceIp);
            entity.AssignedVpnIp = string.IsNullOrWhiteSpace(session.AssignedVpnIp) ? null : IPAddress.Parse(session.AssignedVpnIp);
            entity.NasIdentifier = session.NasIdentifier;
            entity.LastSeenAt = session.LastSeenAt;
            entity.Active = session.Active;
            entity.Authorized = session.Authorized;
            entity.TerminationReason = null;
            entity.EndedAt = null;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> CloseBySessionIdAsync(int userId, string sessionId, DateTimeOffset endedAt, string? terminationReason, CancellationToken cancellationToken)
    {
        var entity = await dbContext.VpnSessions.SingleOrDefaultAsync(x => x.UserId == userId && x.SessionId == sessionId && x.Active, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        entity.Active = false;
        entity.EndedAt = endedAt;
        entity.LastSeenAt = endedAt;
        entity.TerminationReason = terminationReason;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DisconnectAsync(int sessionId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.VpnSessions.SingleOrDefaultAsync(x => x.Id == sessionId && x.Active, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        entity.Active = false;
        entity.Authorized = false;
        entity.EndedAt = DateTimeOffset.UtcNow;
        entity.TerminationReason = "admin_disconnect";
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
