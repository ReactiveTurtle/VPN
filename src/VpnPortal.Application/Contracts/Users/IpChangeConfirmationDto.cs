namespace VpnPortal.Application.Contracts.Users;

public sealed record IpChangeConfirmationDto(
    int Id,
    string RequestedIp,
    string Status,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt,
    string? ConfirmationLink);
