namespace VpnPortal.Application.Contracts.Auth;

public sealed record SessionUserDto(int Id, string Login, string Role, string? Email);
