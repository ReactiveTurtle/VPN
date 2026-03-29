namespace VpnPortal.Application.Contracts.Users;

public sealed record UserDashboardDto(
    int Id,
    string Email,
    string Username,
    bool Active,
    int MaxDevices,
    IReadOnlyCollection<TrustedDeviceDto> Devices,
    IReadOnlyCollection<TrustedIpDto> TrustedIps,
    IReadOnlyCollection<IpChangeConfirmationDto> PendingIpConfirmations,
    IReadOnlyCollection<VpnSessionDto> Sessions);
