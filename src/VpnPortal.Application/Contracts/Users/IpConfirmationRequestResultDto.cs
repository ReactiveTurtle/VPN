namespace VpnPortal.Application.Contracts.Users;

public sealed record IpConfirmationRequestResultDto(
    int ConfirmationId,
    string RequestedIp,
    DateTimeOffset ExpiresAt,
    string ConfirmationLink,
    string Message);
