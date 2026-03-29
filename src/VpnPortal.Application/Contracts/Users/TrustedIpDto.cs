namespace VpnPortal.Application.Contracts.Users;

public sealed record TrustedIpDto(
    int Id,
    string IpAddress,
    string Status,
    DateTimeOffset FirstSeenAt,
    DateTimeOffset? LastSeenAt,
    DateTimeOffset? ApprovedAt);
