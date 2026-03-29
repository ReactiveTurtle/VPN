namespace VpnPortal.Domain.Entities;

public sealed class VpnSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public VpnUser? User { get; set; }
    public int? DeviceId { get; set; }
    public TrustedDevice? Device { get; set; }
    public string SourceIp { get; set; } = string.Empty;
    public string? AssignedVpnIp { get; set; }
    public string? SessionId { get; set; }
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastSeenAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public bool Active { get; set; } = true;
    public bool Authorized { get; set; } = true;
}
