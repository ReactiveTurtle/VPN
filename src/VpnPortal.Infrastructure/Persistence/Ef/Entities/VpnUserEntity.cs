namespace VpnPortal.Infrastructure.Persistence.Ef.Entities;

public sealed class VpnUserEntity
{
    public long Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int MaxDevices { get; set; }
    public bool Active { get; set; }
    public bool EmailConfirmed { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public DateTimeOffset? DeactivatedAt { get; set; }
    public ICollection<TrustedDeviceEntity> Devices { get; set; } = [];
    public ICollection<TrustedIpEntity> TrustedIps { get; set; } = [];
    public ICollection<VpnSessionEntity> Sessions { get; set; } = [];
    public ICollection<VpnDeviceCredentialEntity> DeviceCredentials { get; set; } = [];
    public ICollection<IpChangeConfirmationEntity> IpChangeConfirmations { get; set; } = [];
    public ICollection<VpnRequestEntity> ApprovedRequests { get; set; } = [];
}
