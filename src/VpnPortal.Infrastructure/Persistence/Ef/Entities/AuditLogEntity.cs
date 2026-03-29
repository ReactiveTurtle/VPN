using System.Net;

namespace VpnPortal.Infrastructure.Persistence.Ef.Entities;

public sealed class AuditLogEntity
{
    public long Id { get; set; }
    public string ActorType { get; set; } = string.Empty;
    public long? ActorId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public IPAddress? IpAddress { get; set; }
    public string? DetailsJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
