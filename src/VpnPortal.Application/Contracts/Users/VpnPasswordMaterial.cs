namespace VpnPortal.Application.Contracts.Users;

public sealed record VpnPasswordMaterial(
    string PasswordHash,
    string RadiusNtHash);
