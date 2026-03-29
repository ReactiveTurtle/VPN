namespace VpnPortal.Application.Contracts.Requests;

public sealed record SubmitVpnRequestCommand(string Email, string? Name);
