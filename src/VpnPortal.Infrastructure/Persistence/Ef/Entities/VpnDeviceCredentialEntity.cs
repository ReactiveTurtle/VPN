namespace VpnPortal.Infrastructure.Persistence.Ef.Entities;

public sealed class VpnDeviceCredentialEntity
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public VpnUserEntity User { get; set; } = null!;
    public long DeviceId { get; set; }
    public TrustedDeviceEntity Device { get; set; } = null!;
    public string VpnUsername { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string RadiusNtHash { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? RotatedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
}
