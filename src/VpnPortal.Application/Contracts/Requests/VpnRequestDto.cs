namespace VpnPortal.Application.Contracts.Requests;

public sealed record VpnRequestDto(
    int Id,
    string Email,
    string? Name,
    string Status,
    string? AdminComment,
    DateTimeOffset SubmittedAt,
    DateTimeOffset? ProcessedAt,
    DateTimeOffset? ActivationExpiresAt,
    string? ActivationLink);
