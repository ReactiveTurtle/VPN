using VpnPortal.Application.Interfaces;
using VpnPortal.Domain.Entities;

namespace VpnPortal.Infrastructure.Persistence.InMemory;

public sealed class InMemoryAuditLogRepository : IAuditLogRepository
{
    private readonly List<AuditLogEntry> entries = [];
    private long nextId = 1;

    public Task<AuditLogEntry> AddAsync(AuditLogEntry entry, CancellationToken cancellationToken)
    {
        entry.Id = nextId++;
        entries.Add(entry);
        return Task.FromResult(entry);
    }

    public Task<IReadOnlyCollection<AuditLogEntry>> GetRecentAsync(int take, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<AuditLogEntry>>(entries.OrderByDescending(x => x.CreatedAt).Take(take).ToArray());
    }
}
