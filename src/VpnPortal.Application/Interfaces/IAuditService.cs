namespace VpnPortal.Application.Interfaces;

public interface IAuditService
{
    Task WriteAsync(string actorType, long? actorId, string action, string entityType, string entityId, string? ipAddress, object? details, CancellationToken cancellationToken);
}
