namespace VpnPortal.Application.Contracts.Users;

public sealed record VpnSessionDto(
    int Id,
    string SourceIp,
    string? AssignedVpnIp,
    string? DeviceName,
    DateTimeOffset StartedAt,
    DateTimeOffset? LastSeenAt,
    bool Active,
    bool Authorized);
