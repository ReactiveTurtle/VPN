namespace VpnPortal.Application.Contracts.Account;

public sealed record ActivationCompletedDto(
    int UserId,
    string Email,
    string Username,
    string Message);
