using Dapper;
using VpnPortal.Application.Interfaces;
using VpnPortal.Domain.Entities;

namespace VpnPortal.Infrastructure.Persistence.PostgreSql;

public sealed class PostgreSqlSessionRepository(PostgreSqlConnectionFactory connectionFactory) : ISessionRepository
{
    public async Task<IReadOnlyCollection<VpnSession>> GetRecentAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            select s.id, s.user_id as UserId, s.device_id as DeviceId, s.source_ip::text as SourceIp, s.assigned_vpn_ip::text as AssignedVpnIp,
                   s.session_id as SessionId, s.started_at as StartedAt, s.last_seen_at as LastSeenAt, s.ended_at as EndedAt, s.active, s.authorized,
                   d.id as DeviceId2, d.device_name as DeviceName, u.id as UserId2, u.username as Username
            from vpn_sessions s
            join vpn_users u on u.id = s.user_id
            left join trusted_devices d on d.id = s.device_id
            order by s.started_at desc
            limit 100;
            """;

        using var connection = connectionFactory.Create();
        var rows = await connection.QueryAsync<SessionRow>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return rows.Select(x => x.ToEntity()).ToArray();
    }

    public async Task<bool> DisconnectAsync(int sessionId, CancellationToken cancellationToken)
    {
        const string sql = """
            update vpn_sessions
            set active = false, authorized = false, ended_at = now(), termination_reason = 'admin_disconnect'
            where id = @SessionId and active = true;
            """;

        using var connection = connectionFactory.Create();
        var affected = await connection.ExecuteAsync(new CommandDefinition(sql, new { SessionId = sessionId }, cancellationToken: cancellationToken));
        return affected > 0;
    }

    private sealed record SessionRow(int Id, int UserId, int? DeviceId, string SourceIp, string? AssignedVpnIp, string? SessionId, DateTimeOffset StartedAt, DateTimeOffset? LastSeenAt, DateTimeOffset? EndedAt, bool Active, bool Authorized, int? DeviceId2, string? DeviceName, int UserId2, string Username)
    {
        public VpnSession ToEntity() => new()
        {
            Id = Id,
            UserId = UserId,
            DeviceId = DeviceId,
            SourceIp = SourceIp,
            AssignedVpnIp = AssignedVpnIp,
            SessionId = SessionId,
            StartedAt = StartedAt,
            LastSeenAt = LastSeenAt,
            EndedAt = EndedAt,
            Active = Active,
            Authorized = Authorized,
            Device = DeviceId2 is null ? null : new TrustedDevice { Id = DeviceId2.Value, DeviceName = DeviceName ?? "Unknown device" },
            User = new VpnUser { Id = UserId2, Username = Username }
        };
    }
}
