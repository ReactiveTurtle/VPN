using Dapper;
using VpnPortal.Application.Interfaces;
using VpnPortal.Domain.Entities;
using VpnPortal.Domain.Enums;

namespace VpnPortal.Infrastructure.Persistence.PostgreSql;

public sealed class PostgreSqlUserRepository(PostgreSqlConnectionFactory connectionFactory) : IUserRepository
{
    public async Task<IReadOnlyCollection<VpnUser>> GetAllAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            select id,
                   email,
                   username,
                   password_hash as PasswordHash,
                   max_devices as MaxDevices,
                   active,
                   email_confirmed as EmailConfirmed,
                   created_at as CreatedAt,
                   last_login_at as LastLoginAt
            from vpn_users
            order by created_at desc;
            """;

        using var connection = connectionFactory.Create();
        var rows = await connection.QueryAsync<UserRow>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return rows.Select(x => x.ToEntity([], [], [])).ToArray();
    }

    public async Task<VpnUser?> GetByIdAsync(int userId, CancellationToken cancellationToken)
    {
        const string sql = """
            select id,
                   email,
                   username,
                   password_hash as PasswordHash,
                   max_devices as MaxDevices,
                   active,
                   email_confirmed as EmailConfirmed,
                   created_at as CreatedAt,
                   last_login_at as LastLoginAt
            from vpn_users
            where id = @UserId;
            """;

        using var connection = connectionFactory.Create();
        var row = await connection.QuerySingleOrDefaultAsync<UserRow>(new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));
        return row?.ToEntity([], [], []);
    }

    public async Task<VpnUser?> GetByIdWithRelationsAsync(int userId, CancellationToken cancellationToken)
    {
        const string userSql = """
            select id,
                   email,
                   username,
                   password_hash as PasswordHash,
                   max_devices as MaxDevices,
                   active,
                   email_confirmed as EmailConfirmed,
                   created_at as CreatedAt,
                   last_login_at as LastLoginAt
            from vpn_users
            where id = @UserId;
            """;

        const string devicesSql = """
            select id,
                   user_id as UserId,
                   device_uuid as DeviceUuid,
                   coalesce(device_name, 'Unnamed device') as DeviceName,
                   device_type as DeviceType,
                   platform,
                   status,
                   first_seen_at as FirstSeenAt,
                   last_seen_at as LastSeenAt
            from trusted_devices
            where user_id = @UserId
            order by coalesce(last_seen_at, first_seen_at) desc;
            """;

        const string trustedIpsSql = """
            select id, user_id as UserId, device_id as DeviceId, ip_address::text as IpAddress,
                   status, first_seen_at as FirstSeenAt, last_seen_at as LastSeenAt, approved_at as ApprovedAt, revoked_at as RevokedAt
            from trusted_ips
            where user_id = @UserId
            order by coalesce(last_seen_at, first_seen_at) desc;
            """;

        const string sessionsSql = """
            select s.id,
                   s.user_id as UserId,
                   s.device_id as DeviceId,
                   s.source_ip::text as SourceIp,
                   s.assigned_vpn_ip::text as AssignedVpnIp,
                   s.session_id as SessionId,
                   s.started_at as StartedAt,
                   s.last_seen_at as LastSeenAt,
                   s.ended_at as EndedAt,
                   s.active,
                   s.authorized,
                   d.device_name as DeviceName
            from vpn_sessions s
            left join trusted_devices d on d.id = s.device_id
            where s.user_id = @UserId
            order by s.started_at desc;
            """;

        using var connection = connectionFactory.Create();
        var userRow = await connection.QuerySingleOrDefaultAsync<UserRow>(new CommandDefinition(userSql, new { UserId = userId }, cancellationToken: cancellationToken));
        if (userRow is null)
        {
            return null;
        }

        var devices = (await connection.QueryAsync<DeviceRow>(new CommandDefinition(devicesSql, new { UserId = userId }, cancellationToken: cancellationToken)))
            .Select(x => x.ToEntity())
            .ToArray();

        var trustedIps = (await connection.QueryAsync<TrustedIpRow>(new CommandDefinition(trustedIpsSql, new { UserId = userId }, cancellationToken: cancellationToken)))
            .Select(x => x.ToEntity())
            .ToArray();

        var sessions = (await connection.QueryAsync<SessionRow>(new CommandDefinition(sessionsSql, new { UserId = userId }, cancellationToken: cancellationToken)))
            .Select(x => x.ToEntity())
            .ToArray();

        var deviceLookup = devices.ToDictionary(x => x.Id);
        foreach (var session in sessions)
        {
            if (session.DeviceId is int deviceId && deviceLookup.TryGetValue(deviceId, out var device))
            {
                session.Device = device;
            }
        }

        return userRow.ToEntity(devices, trustedIps, sessions);
    }

    public async Task<VpnUser?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        const string sql = """
            select id,
                   email,
                   username,
                   password_hash as PasswordHash,
                   max_devices as MaxDevices,
                   active,
                   email_confirmed as EmailConfirmed,
                   created_at as CreatedAt,
                   last_login_at as LastLoginAt
            from vpn_users
            where email = @Email;
            """;

        using var connection = connectionFactory.Create();
        var row = await connection.QuerySingleOrDefaultAsync<UserRow>(new CommandDefinition(sql, new { Email = email }, cancellationToken: cancellationToken));
        return row?.ToEntity([], [], []);
    }

    public async Task<VpnUser?> GetByUsernameOrEmailAsync(string login, CancellationToken cancellationToken)
    {
        const string sql = """
            select id,
                   email,
                   username,
                   password_hash as PasswordHash,
                   max_devices as MaxDevices,
                   active,
                   email_confirmed as EmailConfirmed,
                   created_at as CreatedAt,
                   last_login_at as LastLoginAt
            from vpn_users
            where lower(email) = lower(@Login) or lower(username) = lower(@Login)
            limit 1;
            """;

        using var connection = connectionFactory.Create();
        var row = await connection.QuerySingleOrDefaultAsync<UserRow>(new CommandDefinition(sql, new { Login = login }, cancellationToken: cancellationToken));
        return row?.ToEntity([], [], []);
    }

    public async Task<VpnUser> AddAsync(VpnUser user, CancellationToken cancellationToken)
    {
        const string sql = """
            insert into vpn_users (email, username, password_hash, max_devices, active, email_confirmed, created_at, updated_at)
            values (@Email, @Username, @PasswordHash, @MaxDevices, @Active, @EmailConfirmed, @CreatedAt, @UpdatedAt)
            returning id,
                     email,
                     username,
                     password_hash as PasswordHash,
                     max_devices as MaxDevices,
                     active,
                     email_confirmed as EmailConfirmed,
                     created_at as CreatedAt,
                     last_login_at as LastLoginAt;
            """;

        using var connection = connectionFactory.Create();
        var row = await connection.QuerySingleAsync<UserRow>(new CommandDefinition(sql, new
        {
            user.Email,
            user.Username,
            user.PasswordHash,
            user.MaxDevices,
            user.Active,
            user.EmailConfirmed,
            user.CreatedAt,
            UpdatedAt = DateTimeOffset.UtcNow
        }, cancellationToken: cancellationToken));

        return row.ToEntity([], [], []);
    }

    public async Task UpdateAsync(VpnUser user, CancellationToken cancellationToken)
    {
        const string sql = """
            update vpn_users
            set password_hash = @PasswordHash,
                active = @Active,
                email_confirmed = @EmailConfirmed,
                max_devices = @MaxDevices,
                updated_at = @UpdatedAt,
                last_login_at = @LastLoginAt
            where id = @Id;
            """;

        using var connection = connectionFactory.Create();
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            user.Id,
            user.PasswordHash,
            user.Active,
            user.EmailConfirmed,
            user.MaxDevices,
            UpdatedAt = DateTimeOffset.UtcNow,
            user.LastLoginAt
        }, cancellationToken: cancellationToken));
    }

    private sealed record UserRow(int Id, string Email, string Username, string PasswordHash, int MaxDevices, bool Active, bool EmailConfirmed, DateTimeOffset CreatedAt, DateTimeOffset? LastLoginAt)
    {
        public VpnUser ToEntity(IReadOnlyCollection<TrustedDevice> devices, IReadOnlyCollection<TrustedIp> trustedIps, IReadOnlyCollection<VpnSession> sessions)
        {
            return new VpnUser
            {
                Id = Id,
                Email = Email,
                Username = Username,
                PasswordHash = PasswordHash,
                MaxDevices = MaxDevices,
                Active = Active,
                EmailConfirmed = EmailConfirmed,
                CreatedAt = CreatedAt,
                LastLoginAt = LastLoginAt,
                Devices = devices.ToList(),
                TrustedIps = trustedIps.ToList(),
                Sessions = sessions.ToList()
            };
        }
    }

    private sealed record TrustedIpRow(int Id, int UserId, int? DeviceId, string IpAddress, string Status, DateTimeOffset FirstSeenAt, DateTimeOffset? LastSeenAt, DateTimeOffset? ApprovedAt, DateTimeOffset? RevokedAt)
    {
        public TrustedIp ToEntity()
        {
            return new TrustedIp
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

    private sealed record DeviceRow(int Id, int UserId, string DeviceUuid, string DeviceName, string DeviceType, string Platform, string Status, DateTimeOffset FirstSeenAt, DateTimeOffset? LastSeenAt)
    {
        public TrustedDevice ToEntity()
        {
            return new TrustedDevice
            {
                Id = Id,
                UserId = UserId,
                DeviceUuid = DeviceUuid,
                DeviceName = DeviceName,
                DeviceType = DeviceType,
                Platform = Platform,
                Status = Enum.Parse<DeviceStatus>(Status, true),
                FirstSeenAt = FirstSeenAt,
                LastSeenAt = LastSeenAt
            };
        }
    }

    private sealed record SessionRow(int Id, int UserId, int? DeviceId, string SourceIp, string? AssignedVpnIp, string? SessionId, DateTimeOffset StartedAt, DateTimeOffset? LastSeenAt, DateTimeOffset? EndedAt, bool Active, bool Authorized, string? DeviceName)
    {
        public VpnSession ToEntity()
        {
            return new VpnSession
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
                Device = DeviceId is null || DeviceName is null
                    ? null
                    : new TrustedDevice { Id = DeviceId.Value, DeviceName = DeviceName }
            };
        }
    }
}
