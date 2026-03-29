namespace VpnPortal.Application.Contracts.Users;

public sealed record TrustedDeviceDto(
    int Id,
    string DeviceName,
    string DeviceType,
    string Platform,
    string Status,
    string? VpnUsername,
    string? CredentialStatus,
    DateTimeOffset? CredentialRotatedAt,
    DateTimeOffset FirstSeenAt,
    DateTimeOffset? LastSeenAt);
