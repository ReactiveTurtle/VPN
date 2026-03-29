namespace VpnPortal.Application.Contracts.Admin;

public sealed record AdminSessionDto(
    int Id,
    int UserId,
    string Username,
    string? DeviceName,
    string SourceIp,
    string? AssignedVpnIp,
    DateTimeOffset StartedAt,
    DateTimeOffset? LastSeenAt,
    bool Active,
    bool Authorized);
