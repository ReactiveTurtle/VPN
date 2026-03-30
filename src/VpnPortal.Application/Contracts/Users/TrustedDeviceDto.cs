namespace VpnPortal.Application.Contracts.Users;

public sealed record TrustedDeviceDto(
    int Id,
    string DeviceName,
    string Status,
    string? VpnUsername,
    string? CredentialStatus,
    DateTimeOffset? CredentialRotatedAt,
    string? BoundIpAddress,
    DateTimeOffset? BoundIpLastSeenAt,
    DateTimeOffset FirstSeenAt,
    DateTimeOffset? LastSeenAt);
