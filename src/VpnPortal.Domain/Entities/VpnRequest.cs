using VpnPortal.Domain.Enums;

namespace VpnPortal.Domain.Entities;

public sealed class VpnRequest
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? RequestedByIp { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public string? AdminComment { get; set; }
    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAt { get; set; }
}
