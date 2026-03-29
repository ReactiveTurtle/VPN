using VpnPortal.Domain.Entities;

namespace VpnPortal.Application.Interfaces;

public interface IAuditLogRepository
{
    Task<AuditLogEntry> AddAsync(AuditLogEntry entry, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AuditLogEntry>> GetRecentAsync(int take, CancellationToken cancellationToken);
}
