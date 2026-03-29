namespace VpnPortal.Application.Contracts.Internal;

public sealed record VpnAccountingEventCommand(
    string EventType,
    string VpnUsername,
    string SessionId,
    string SourceIp,
    string? AssignedVpnIp,
    string? NasIdentifier,
    DateTimeOffset? OccurredAt,
    string? TerminationReason);
