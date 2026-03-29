using System.Text.Json;
using VpnPortal.Application.Interfaces;
using VpnPortal.Domain.Entities;

namespace VpnPortal.Infrastructure.Services;

public sealed class AuditService(IAuditLogRepository repository) : IAuditService
{
    public Task WriteAsync(string actorType, long? actorId, string action, string entityType, string entityId, string? ipAddress, object? details, CancellationToken cancellationToken)
    {
        return repository.AddAsync(new AuditLogEntry
        {
            ActorType = actorType,
            ActorId = actorId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            IpAddress = ipAddress,
            DetailsJson = details is null ? null : JsonSerializer.Serialize(details),
            CreatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);
    }
}
