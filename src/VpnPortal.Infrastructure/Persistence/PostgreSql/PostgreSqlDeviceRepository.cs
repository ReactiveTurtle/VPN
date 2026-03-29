using Dapper;
using VpnPortal.Application.Interfaces;
using VpnPortal.Domain.Entities;
using VpnPortal.Domain.Enums;

namespace VpnPortal.Infrastructure.Persistence.PostgreSql;

public sealed class PostgreSqlDeviceRepository(PostgreSqlConnectionFactory connectionFactory) : IDeviceRepository
{
    public async Task<TrustedDevice?> GetByIdAsync(int deviceId, CancellationToken cancellationToken)
    {
        const string sql = """
            select id, user_id as UserId, device_uuid as DeviceUuid, coalesce(device_name, 'Unnamed device') as DeviceName,
                   device_type as DeviceType, platform, status, first_seen_at as FirstSeenAt, last_seen_at as LastSeenAt
            from trusted_devices
            where id = @DeviceId;
            """;

        using var connection = connectionFactory.Create();
        var row = await connection.QuerySingleOrDefaultAsync<Row>(new CommandDefinition(sql, new { DeviceId = deviceId }, cancellationToken: cancellationToken));
        return row?.ToEntity();
    }

    public async Task<TrustedDevice> AddAsync(TrustedDevice device, CancellationToken cancellationToken)
    {
        const string sql = """
            insert into trusted_devices (user_id, device_uuid, device_name, device_type, platform, status, first_seen_at, last_seen_at, approved_at, revoked_at)
            values (@UserId, @DeviceUuid, @DeviceName, @DeviceType, @Platform, @Status, @FirstSeenAt, @LastSeenAt, @ApprovedAt, @RevokedAt)
            returning id, user_id as UserId, device_uuid as DeviceUuid, coalesce(device_name, 'Unnamed device') as DeviceName,
                      device_type as DeviceType, platform, status, first_seen_at as FirstSeenAt, last_seen_at as LastSeenAt;
            """;

        using var connection = connectionFactory.Create();
        var row = await connection.QuerySingleAsync<Row>(new CommandDefinition(sql, new
        {
            device.UserId,
            device.DeviceUuid,
            device.DeviceName,
            device.DeviceType,
            device.Platform,
            Status = device.Status.ToString().ToLowerInvariant(),
            device.FirstSeenAt,
            device.LastSeenAt,
            ApprovedAt = device.Status == DeviceStatus.Active ? device.FirstSeenAt : (DateTimeOffset?)null,
            RevokedAt = (DateTimeOffset?)null
        }, cancellationToken: cancellationToken));

        return row.ToEntity();
    }

    public async Task<bool> RevokeAsync(int userId, int deviceId, CancellationToken cancellationToken)
    {
        const string sql = """
            update trusted_devices
            set status = 'revoked', revoked_at = now()
            where id = @DeviceId and user_id = @UserId;
            """;

        using var connection = connectionFactory.Create();
        var affected = await connection.ExecuteAsync(new CommandDefinition(sql, new { DeviceId = deviceId, UserId = userId }, cancellationToken: cancellationToken));
        return affected > 0;
    }

    private sealed record Row(int Id, int UserId, string DeviceUuid, string DeviceName, string DeviceType, string Platform, string Status, DateTimeOffset FirstSeenAt, DateTimeOffset? LastSeenAt)
    {
        public TrustedDevice ToEntity() => new()
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
