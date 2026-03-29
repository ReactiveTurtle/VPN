namespace VpnPortal.Application.Contracts.Admin;

public sealed record AdminUserDto(
    int Id,
    string Email,
    string Username,
    bool Active,
    bool EmailConfirmed,
    int MaxDevices,
    int DeviceCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt);
