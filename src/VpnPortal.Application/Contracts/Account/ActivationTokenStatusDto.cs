namespace VpnPortal.Application.Contracts.Account;

public sealed record ActivationTokenStatusDto(
    bool Valid,
    bool Used,
    string? Email,
    DateTimeOffset? ExpiresAt,
    string Message);
