using Dapper;
using VpnPortal.Application.Interfaces;
using VpnPortal.Domain.Entities;

namespace VpnPortal.Infrastructure.Persistence.PostgreSql;

public sealed class PostgreSqlAuditLogRepository(PostgreSqlConnectionFactory connectionFactory) : IAuditLogRepository
{
    public async Task<AuditLogEntry> AddAsync(AuditLogEntry entry, CancellationToken cancellationToken)
    {
        const string sql = """
            insert into audit_log (actor_type, actor_id, action, entity_type, entity_id, ip_address, details, created_at)
            values (@ActorType, @ActorId, @Action, @EntityType, @EntityId, cast(@IpAddress as inet), cast(@DetailsJson as jsonb), @CreatedAt)
            returning id, actor_type as ActorType, actor_id as ActorId, action, entity_type as EntityType, entity_id as EntityId, ip_address::text as IpAddress, details::text as DetailsJson, created_at as CreatedAt;
            """;

        using var connection = connectionFactory.Create();
        return await connection.QuerySingleAsync<AuditLogEntry>(new CommandDefinition(sql, entry, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyCollection<AuditLogEntry>> GetRecentAsync(int take, CancellationToken cancellationToken)
    {
        const string sql = """
            select id, actor_type as ActorType, actor_id as ActorId, action, entity_type as EntityType, entity_id as EntityId,
                   ip_address::text as IpAddress, details::text as DetailsJson, created_at as CreatedAt
            from audit_log
            order by created_at desc
            limit @Take;
            """;

        using var connection = connectionFactory.Create();
        var rows = await connection.QueryAsync<AuditLogEntry>(new CommandDefinition(sql, new { Take = take }, cancellationToken: cancellationToken));
        return rows.ToArray();
    }
}
