namespace VpnPortal.Infrastructure.Persistence.Ef.Entities;

public sealed class SuperAdminEntity
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public ICollection<VpnRequestEntity> ProcessedRequests { get; set; } = [];
    public ICollection<AccountTokenEntity> CreatedTokens { get; set; } = [];
}
