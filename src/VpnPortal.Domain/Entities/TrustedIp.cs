using VpnPortal.Domain.Enums;

namespace VpnPortal.Domain.Entities;

public sealed class TrustedIp
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? DeviceId { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public TrustedIpStatus Status { get; set; } = TrustedIpStatus.Active;
    public DateTimeOffset FirstSeenAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastSeenAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}
