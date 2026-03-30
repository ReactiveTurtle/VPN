using VpnPortal.Domain.Enums;

namespace VpnPortal.Domain.Entities;

public sealed class IpChangeConfirmation
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? DeviceId { get; set; }
    public string RequestedIp { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public IpChangeConfirmationStatus Status { get; set; } = IpChangeConfirmationStatus.Pending;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ConfirmedAt { get; set; }

    public void Confirm(DateTimeOffset confirmedAt)
    {
        Status = IpChangeConfirmationStatus.Confirmed;
        ConfirmedAt = confirmedAt;
    }
}
