using System.Net;

namespace VpnPortal.Infrastructure.Persistence.Ef.Entities;

public sealed class VpnSessionEntity
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public VpnUserEntity User { get; set; } = null!;
    public long? DeviceId { get; set; }
    public TrustedDeviceEntity? Device { get; set; }
    public IPAddress SourceIp { get; set; } = IPAddress.None;
    public IPAddress? AssignedVpnIp { get; set; }
    public string? NasIdentifier { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public string? TerminationReason { get; set; }
    public bool Active { get; set; }
    public bool Authorized { get; set; }
}
