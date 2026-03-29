namespace VpnPortal.Application.Contracts.System;

public sealed record AppStatusDto(string Name, string Version, bool DatabaseConfigured, bool DevelopmentMode);
