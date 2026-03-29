namespace VpnPortal.Application.Contracts.Account;

public sealed record ActivateAccountCommand(string Token, string Password);
