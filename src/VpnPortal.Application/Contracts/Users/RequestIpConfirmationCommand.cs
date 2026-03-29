namespace VpnPortal.Application.Contracts.Users;

public sealed record RequestIpConfirmationCommand(string RequestedIp, int? DeviceId);
