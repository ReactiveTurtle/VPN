namespace VpnPortal.Application.Contracts.Users;

public sealed record TrustedDeviceDto(
    int Id,
    string DeviceName,
    string DeviceType,
    string Platform,
    string Status,
    DateTimeOffset FirstSeenAt,
    DateTimeOffset? LastSeenAt);
