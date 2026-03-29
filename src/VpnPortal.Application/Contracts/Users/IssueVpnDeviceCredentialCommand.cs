namespace VpnPortal.Application.Contracts.Users;

public sealed record IssueVpnDeviceCredentialCommand(
    string DeviceName,
    string DeviceType,
    string Platform);
