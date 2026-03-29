namespace VpnPortal.Application.Contracts.Users;

public sealed record VpnOnboardingInstructionDto(
    string Platform,
    string Title,
    string Summary,
    IReadOnlyCollection<string> Steps,
    string CredentialLabel);
