namespace VpnPortal.Application.Contracts.Account;

public sealed record ActivationTokenDto(
    string ActivationToken,
    string ActivationLink,
    DateTimeOffset ExpiresAt);
