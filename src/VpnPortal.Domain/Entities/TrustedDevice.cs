using VpnPortal.Domain.Enums;

namespace VpnPortal.Domain.Entities;

public sealed class TrustedDevice
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public VpnUser? User { get; set; }
    public string DeviceUuid { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public DeviceStatus Status { get; set; } = DeviceStatus.Pending;
    public DateTimeOffset FirstSeenAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastSeenAt { get; set; }
    public VpnDeviceCredential? ActiveCredential { get; set; }
}
