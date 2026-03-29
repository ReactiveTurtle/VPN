using VpnPortal.Domain.Enums;

namespace VpnPortal.Domain.Entities;

public sealed class AccountToken
{
    public int Id { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public AccountTokenPurpose Purpose { get; set; } = AccountTokenPurpose.AccountActivation;
    public DateTimeOffset ExpiresAt { get; set; }
    public bool Used { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
