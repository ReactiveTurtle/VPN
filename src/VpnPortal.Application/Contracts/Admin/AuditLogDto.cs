namespace VpnPortal.Application.Contracts.Admin;

public sealed record AuditLogDto(
    long Id,
    string ActorType,
    long? ActorId,
    string Action,
    string EntityType,
    string EntityId,
    string? IpAddress,
    string? DetailsJson,
    DateTimeOffset CreatedAt);
