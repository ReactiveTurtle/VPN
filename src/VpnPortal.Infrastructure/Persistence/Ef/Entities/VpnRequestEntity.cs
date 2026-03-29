namespace VpnPortal.Infrastructure.Persistence.Ef.Entities;

public sealed class VpnRequestEntity
{
    public long Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? RequestedByIp { get; set; }
    public string Status { get; set; } = "pending";
    public DateTimeOffset SubmittedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public long? ProcessedByAdminId { get; set; }
    public SuperAdminEntity? ProcessedByAdmin { get; set; }
    public long? ApprovedUserId { get; set; }
    public VpnUserEntity? ApprovedUser { get; set; }
    public string? AdminComment { get; set; }
}
