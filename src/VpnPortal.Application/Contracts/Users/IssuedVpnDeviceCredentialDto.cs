namespace VpnPortal.Application.Contracts.Users;

public sealed record IssuedVpnDeviceCredentialDto(
    int DeviceId,
    string DeviceName,
    string VpnUsername,
    string VpnPassword,
    string Message);
