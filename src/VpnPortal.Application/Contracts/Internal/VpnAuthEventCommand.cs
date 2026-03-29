namespace VpnPortal.Application.Contracts.Internal;

public sealed record VpnAuthEventCommand(
    string EventType,
    string VpnUsername,
    string SourceIp,
    string? SessionId,
    string? Reason,
    DateTimeOffset? OccurredAt);
