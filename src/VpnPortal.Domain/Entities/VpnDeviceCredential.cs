using VpnPortal.Domain.Enums;

namespace VpnPortal.Domain.Entities;

public sealed class VpnDeviceCredential
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int DeviceId { get; set; }
    public TrustedDevice? Device { get; set; }
    public string VpnUsername { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public VpnDeviceCredentialStatus Status { get; set; } = VpnDeviceCredentialStatus.Active;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RotatedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
}
