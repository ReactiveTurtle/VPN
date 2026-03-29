using Dapper;
using VpnPortal.Application.Interfaces;
using VpnPortal.Domain.Entities;
using VpnPortal.Domain.Enums;

namespace VpnPortal.Infrastructure.Persistence.PostgreSql;

public sealed class PostgreSqlDeviceCredentialRepository(PostgreSqlConnectionFactory connectionFactory) : IDeviceCredentialRepository
{
    public async Task<VpnDeviceCredential?> GetActiveByDeviceIdAsync(int deviceId, CancellationToken cancellationToken)
    {
        const string sql = """
            select id,
                   user_id as UserId,
                   device_id as DeviceId,
                   vpn_username as VpnUsername,
                   password_hash as PasswordHash,
                   radius_nt_hash as RadiusNtHash,
                   status,
                   created_at as CreatedAt,
                   rotated_at as RotatedAt,
                   revoked_at as RevokedAt,
                   last_used_at as LastUsedAt
            from vpn_device_credentials
            where device_id = @DeviceId and status = 'active'
            limit 1;
            """;

        using var connection = connectionFactory.Create();
        var row = await connection.QuerySingleOrDefaultAsync<Row>(new CommandDefinition(sql, new { DeviceId = deviceId }, cancellationToken: cancellationToken));
        return row?.ToEntity();
    }

    public async Task<VpnDeviceCredential> AddAsync(VpnDeviceCredential credential, CancellationToken cancellationToken)
    {
        const string sql = """
            insert into vpn_device_credentials (user_id, device_id, vpn_username, password_hash, radius_nt_hash, status, created_at, rotated_at, revoked_at, last_used_at)
            values (@UserId, @DeviceId, @VpnUsername, @PasswordHash, @RadiusNtHash, @Status, @CreatedAt, @RotatedAt, @RevokedAt, @LastUsedAt)
            returning id,
                      user_id as UserId,
                      device_id as DeviceId,
                      vpn_username as VpnUsername,
                      password_hash as PasswordHash,
                      radius_nt_hash as RadiusNtHash,
                      status,
                      created_at as CreatedAt,
                      rotated_at as RotatedAt,
                      revoked_at as RevokedAt,
                      last_used_at as LastUsedAt;
            """;

        using var connection = connectionFactory.Create();
        var row = await connection.QuerySingleAsync<Row>(new CommandDefinition(sql, new
        {
            credential.UserId,
            credential.DeviceId,
            credential.VpnUsername,
            credential.PasswordHash,
            credential.RadiusNtHash,
            Status = credential.Status.ToString().ToLowerInvariant(),
            credential.CreatedAt,
            credential.RotatedAt,
            credential.RevokedAt,
            credential.LastUsedAt
        }, cancellationToken: cancellationToken));

        return row.ToEntity();
    }

    public async Task UpdateAsync(VpnDeviceCredential credential, CancellationToken cancellationToken)
    {
        const string sql = """
            update vpn_device_credentials
            set password_hash = @PasswordHash,
                radius_nt_hash = @RadiusNtHash,
                status = @Status,
                rotated_at = @RotatedAt,
                revoked_at = @RevokedAt,
                last_used_at = @LastUsedAt
            where id = @Id;
            """;

        using var connection = connectionFactory.Create();
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            credential.Id,
            credential.PasswordHash,
            credential.RadiusNtHash,
            Status = credential.Status.ToString().ToLowerInvariant(),
            credential.RotatedAt,
            credential.RevokedAt,
            credential.LastUsedAt
        }, cancellationToken: cancellationToken));
    }

    public async Task<bool> RevokeActiveByDeviceIdAsync(int userId, int deviceId, CancellationToken cancellationToken)
    {
        const string sql = """
            update vpn_device_credentials
            set status = 'revoked',
                revoked_at = now()
            where user_id = @UserId and device_id = @DeviceId and status = 'active';
            """;

        using var connection = connectionFactory.Create();
        var affected = await connection.ExecuteAsync(new CommandDefinition(sql, new { UserId = userId, DeviceId = deviceId }, cancellationToken: cancellationToken));
        return affected > 0;
    }

    private sealed record Row(int Id, int UserId, int DeviceId, string VpnUsername, string PasswordHash, string RadiusNtHash, string Status, DateTimeOffset CreatedAt, DateTimeOffset? RotatedAt, DateTimeOffset? RevokedAt, DateTimeOffset? LastUsedAt)
    {
        public VpnDeviceCredential ToEntity() => new()
        {
            Id = Id,
            UserId = UserId,
            DeviceId = DeviceId,
            VpnUsername = VpnUsername,
            PasswordHash = PasswordHash,
            RadiusNtHash = RadiusNtHash,
            Status = Enum.Parse<VpnDeviceCredentialStatus>(Status, true),
            CreatedAt = CreatedAt,
            RotatedAt = RotatedAt,
            RevokedAt = RevokedAt,
            LastUsedAt = LastUsedAt
        };
    }
}
