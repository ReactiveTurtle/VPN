using System.Net;

namespace VpnPortal.Infrastructure.Persistence.Ef.Entities;

public sealed class TrustedIpEntity
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public VpnUserEntity User { get; set; } = null!;
    public long? DeviceId { get; set; }
    public TrustedDeviceEntity? Device { get; set; }
    public IPAddress IpAddress { get; set; } = IPAddress.None;
    public string Status { get; set; } = "active";
    public DateTimeOffset FirstSeenAt { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}
