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

    public void Approve(string? adminComment, DateTimeOffset processedAt)
    {
        Status = RequestStatus.Approved;
        AdminComment = string.IsNullOrWhiteSpace(adminComment) ? "Approved" : adminComment.Trim();
        ProcessedAt = processedAt;
    }

    public void Reject(string? adminComment, DateTimeOffset processedAt)
    {
        Status = RequestStatus.Rejected;
        AdminComment = string.IsNullOrWhiteSpace(adminComment) ? "Rejected" : adminComment.Trim();
        ProcessedAt = processedAt;
    }
}
