namespace VpnPortal.Application.Contracts.Users;

public sealed record UserDashboardDto(
    int Id,
    string Email,
    bool Active,
    int MaxDevices,
    IReadOnlyCollection<VpnOnboardingInstructionDto> PlatformGuides,
    IReadOnlyCollection<TrustedDeviceDto> Devices,
    IReadOnlyCollection<VpnSessionDto> Sessions);
