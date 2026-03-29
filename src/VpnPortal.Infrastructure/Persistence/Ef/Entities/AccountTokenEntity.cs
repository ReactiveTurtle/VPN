namespace VpnPortal.Infrastructure.Persistence.Ef.Entities;

public sealed class AccountTokenEntity
{
    public long Id { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public bool Used { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public long? CreatedByAdminId { get; set; }
    public SuperAdminEntity? CreatedByAdmin { get; set; }
}
