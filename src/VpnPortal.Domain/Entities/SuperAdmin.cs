namespace VpnPortal.Domain.Entities;

public sealed class SuperAdmin
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastLoginAt { get; set; }

    public void MarkLogin(DateTimeOffset occurredAt)
    {
        LastLoginAt = occurredAt;
    }
}
