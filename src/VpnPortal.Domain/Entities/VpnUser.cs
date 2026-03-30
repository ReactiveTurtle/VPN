namespace VpnPortal.Domain.Entities;

public sealed class VpnUser
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int MaxDevices { get; set; } = 2;
    public bool Active { get; set; } = true;
    public bool EmailConfirmed { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastLoginAt { get; set; }
    public ICollection<TrustedDevice> Devices { get; set; } = new List<TrustedDevice>();
    public ICollection<TrustedIp> TrustedIps { get; set; } = new List<TrustedIp>();
    public ICollection<VpnSession> Sessions { get; set; } = new List<VpnSession>();

    public void ActivateAccount(string passwordHash)
    {
        PasswordHash = passwordHash;
        EmailConfirmed = true;
        Active = true;
    }

    public void SetMaxDevices(int maxDevices)
    {
        if (maxDevices < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDevices));
        }

        MaxDevices = maxDevices;
    }

    public void SetActive(bool active)
    {
        Active = active;
    }

    public void MarkLogin(DateTimeOffset occurredAt)
    {
        LastLoginAt = occurredAt;
    }
}
