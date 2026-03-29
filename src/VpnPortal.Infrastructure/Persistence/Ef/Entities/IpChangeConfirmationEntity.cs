namespace VpnPortal.Infrastructure.Persistence.Ef.Entities;

public sealed class IpChangeConfirmationEntity
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public VpnUserEntity User { get; set; } = null!;
    public long? DeviceId { get; set; }
    public TrustedDeviceEntity? Device { get; set; }
    public string RequestedIp { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
}
