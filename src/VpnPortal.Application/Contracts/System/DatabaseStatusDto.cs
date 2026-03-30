namespace VpnPortal.Application.Contracts.System;

public sealed record DatabaseStatusDto(bool Configured, bool CanConnect, string? Error);
