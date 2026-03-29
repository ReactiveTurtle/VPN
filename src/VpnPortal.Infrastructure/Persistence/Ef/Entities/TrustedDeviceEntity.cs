namespace VpnPortal.Infrastructure.Persistence.Ef.Entities;

public sealed class TrustedDeviceEntity
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public VpnUserEntity User { get; set; } = null!;
    public string DeviceUuid { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
    public string DeviceType { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public DateTimeOffset FirstSeenAt { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public VpnDeviceCredentialEntity? Credential { get; set; }
    public ICollection<TrustedIpEntity> TrustedIps { get; set; } = [];
    public ICollection<VpnSessionEntity> Sessions { get; set; } = [];
    public ICollection<IpChangeConfirmationEntity> IpChangeConfirmations { get; set; } = [];
}
