using Dapper;
using VpnPortal.Application.Interfaces;
using VpnPortal.Domain.Entities;
using VpnPortal.Domain.Enums;

namespace VpnPortal.Infrastructure.Persistence.PostgreSql;

public sealed class PostgreSqlTrustedIpRepository(PostgreSqlConnectionFactory connectionFactory) : ITrustedIpRepository
{
    public async Task<IReadOnlyCollection<TrustedIp>> GetByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        const string sql = """
            select id, user_id as UserId, device_id as DeviceId, ip_address::text as IpAddress,
                   status, first_seen_at as FirstSeenAt, last_seen_at as LastSeenAt, approved_at as ApprovedAt, revoked_at as RevokedAt
            from trusted_ips
            where user_id = @UserId
            order by coalesce(last_seen_at, first_seen_at) desc;
            """;

        using var connection = connectionFactory.Create();
        var rows = await connection.QueryAsync<Row>(new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));
        return rows.Select(x => x.ToEntity()).ToArray();
    }

    public async Task<TrustedIp?> GetByUserAndIpAsync(int userId, string ipAddress, CancellationToken cancellationToken)
    {
        const string sql = """
            select id, user_id as UserId, device_id as DeviceId, ip_address::text as IpAddress,
                   status, first_seen_at as FirstSeenAt, last_seen_at as LastSeenAt, approved_at as ApprovedAt, revoked_at as RevokedAt
            from trusted_ips
            where user_id = @UserId and ip_address = cast(@IpAddress as inet)
            limit 1;
            """;

        using var connection = connectionFactory.Create();
        var row = await connection.QuerySingleOrDefaultAsync<Row>(new CommandDefinition(sql, new { UserId = userId, IpAddress = ipAddress }, cancellationToken: cancellationToken));
        return row?.ToEntity();
    }

    public async Task<TrustedIp> AddAsync(TrustedIp trustedIp, CancellationToken cancellationToken)
    {
        const string sql = """
            insert into trusted_ips (user_id, device_id, ip_address, status, first_seen_at, last_seen_at, approved_at, revoked_at)
            values (@UserId, @DeviceId, cast(@IpAddress as inet), @Status, @FirstSeenAt, @LastSeenAt, @ApprovedAt, @RevokedAt)
            returning id, user_id as UserId, device_id as DeviceId, ip_address::text as IpAddress,
                      status, first_seen_at as FirstSeenAt, last_seen_at as LastSeenAt, approved_at as ApprovedAt, revoked_at as RevokedAt;
            """;

        using var connection = connectionFactory.Create();
        var row = await connection.QuerySingleAsync<Row>(new CommandDefinition(sql, new
        {
            trustedIp.UserId,
            trustedIp.DeviceId,
            trustedIp.IpAddress,
            Status = trustedIp.Status.ToString().ToLowerInvariant(),
            trustedIp.FirstSeenAt,
            trustedIp.LastSeenAt,
            trustedIp.ApprovedAt,
            trustedIp.RevokedAt
        }, cancellationToken: cancellationToken));
        return row.ToEntity();
    }

    public async Task UpdateAsync(TrustedIp trustedIp, CancellationToken cancellationToken)
    {
        const string sql = """
            update trusted_ips
            set status = @Status,
                last_seen_at = @LastSeenAt,
                approved_at = @ApprovedAt,
                revoked_at = @RevokedAt,
                device_id = @DeviceId
            where id = @Id;
            """;

        using var connection = connectionFactory.Create();
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            trustedIp.Id,
            Status = trustedIp.Status.ToString().ToLowerInvariant(),
            trustedIp.LastSeenAt,
            trustedIp.ApprovedAt,
            trustedIp.RevokedAt,
            trustedIp.DeviceId
        }, cancellationToken: cancellationToken));
    }

    private sealed record Row(int Id, int UserId, int? DeviceId, string IpAddress, string Status, DateTimeOffset FirstSeenAt, DateTimeOffset? LastSeenAt, DateTimeOffset? ApprovedAt, DateTimeOffset? RevokedAt)
    {
        public TrustedIp ToEntity() => new()
        {
            Id = Id,
            UserId = UserId,
            DeviceId = DeviceId,
            IpAddress = IpAddress,
            Status = Enum.Parse<TrustedIpStatus>(Status, true),
            FirstSeenAt = FirstSeenAt,
            LastSeenAt = LastSeenAt,
            ApprovedAt = ApprovedAt,
            RevokedAt = RevokedAt
        };
    }
}
