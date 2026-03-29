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

    public async Task RecordAuthorizedAsync(VpnSession session, CancellationToken cancellationToken)
    {
        const string updateSql = """
            update vpn_sessions
            set source_ip = cast(@SourceIp as inet),
                assigned_vpn_ip = cast(@AssignedVpnIp as inet),
                nas_identifier = @NasIdentifier,
                last_seen_at = @LastSeenAt,
                active = @Active,
                authorized = @Authorized,
                termination_reason = null,
                ended_at = null
            where user_id = @UserId and session_id = @SessionId;
            """;

        const string insertSql = """
            insert into vpn_sessions (user_id, device_id, source_ip, assigned_vpn_ip, nas_identifier, session_id, started_at, last_seen_at, active, authorized)
            values (@UserId, @DeviceId, cast(@SourceIp as inet), cast(@AssignedVpnIp as inet), @NasIdentifier, @SessionId, @StartedAt, @LastSeenAt, @Active, @Authorized);
            """;

        using var connection = connectionFactory.Create();
        var parameters = new
        {
            session.UserId,
            session.DeviceId,
            session.SourceIp,
            session.AssignedVpnIp,
            session.NasIdentifier,
            session.SessionId,
            session.StartedAt,
            session.LastSeenAt,
            session.Active,
            session.Authorized
        };

        var affected = await connection.ExecuteAsync(new CommandDefinition(updateSql, parameters, cancellationToken: cancellationToken));
        if (affected == 0)
        {
            await connection.ExecuteAsync(new CommandDefinition(insertSql, parameters, cancellationToken: cancellationToken));
        }
    }

    public async Task<bool> CloseBySessionIdAsync(int userId, string sessionId, DateTimeOffset endedAt, string? terminationReason, CancellationToken cancellationToken)
    {
        const string sql = """
            update vpn_sessions
            set active = false,
                ended_at = @EndedAt,
                last_seen_at = @EndedAt,
                termination_reason = @TerminationReason
            where user_id = @UserId and session_id = @SessionId and active = true;
            """;

        using var connection = connectionFactory.Create();
        var affected = await connection.ExecuteAsync(new CommandDefinition(sql, new { UserId = userId, SessionId = sessionId, EndedAt = endedAt, TerminationReason = terminationReason }, cancellationToken: cancellationToken));
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
