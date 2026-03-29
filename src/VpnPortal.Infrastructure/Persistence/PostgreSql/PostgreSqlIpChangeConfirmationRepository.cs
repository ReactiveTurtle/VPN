using Dapper;
using VpnPortal.Application.Interfaces;
using VpnPortal.Domain.Entities;
using VpnPortal.Domain.Enums;

namespace VpnPortal.Infrastructure.Persistence.PostgreSql;

public sealed class PostgreSqlIpChangeConfirmationRepository(PostgreSqlConnectionFactory connectionFactory) : IIpChangeConfirmationRepository
{
    public async Task<IReadOnlyCollection<IpChangeConfirmation>> GetPendingByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        const string sql = """
            select id, user_id as UserId, device_id as DeviceId, requested_ip::text as RequestedIp,
                   token_hash as TokenHash, status, expires_at as ExpiresAt, created_at as CreatedAt, confirmed_at as ConfirmedAt
            from ip_change_confirmations
            where user_id = @UserId
            order by created_at desc;
            """;

        using var connection = connectionFactory.Create();
        var rows = await connection.QueryAsync<Row>(new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));
        return rows.Select(x => x.ToEntity()).ToArray();
    }

    public async Task<IpChangeConfirmation> AddAsync(IpChangeConfirmation confirmation, CancellationToken cancellationToken)
    {
        const string sql = """
            insert into ip_change_confirmations (user_id, device_id, requested_ip, token_hash, status, expires_at, created_at, confirmed_at)
            values (@UserId, @DeviceId, cast(@RequestedIp as inet), @TokenHash, @Status, @ExpiresAt, @CreatedAt, @ConfirmedAt)
            returning id, user_id as UserId, device_id as DeviceId, requested_ip::text as RequestedIp,
                      token_hash as TokenHash, status, expires_at as ExpiresAt, created_at as CreatedAt, confirmed_at as ConfirmedAt;
            """;

        using var connection = connectionFactory.Create();
        var row = await connection.QuerySingleAsync<Row>(new CommandDefinition(sql, new
        {
            confirmation.UserId,
            confirmation.DeviceId,
            confirmation.RequestedIp,
            confirmation.TokenHash,
            Status = confirmation.Status.ToString().ToLowerInvariant(),
            confirmation.ExpiresAt,
            confirmation.CreatedAt,
            confirmation.ConfirmedAt
        }, cancellationToken: cancellationToken));
        return row.ToEntity();
    }

    public async Task<IpChangeConfirmation?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        const string sql = """
            select id, user_id as UserId, device_id as DeviceId, requested_ip::text as RequestedIp,
                   token_hash as TokenHash, status, expires_at as ExpiresAt, created_at as CreatedAt, confirmed_at as ConfirmedAt
            from ip_change_confirmations
            where token_hash = @TokenHash
            limit 1;
            """;

        using var connection = connectionFactory.Create();
        var row = await connection.QuerySingleOrDefaultAsync<Row>(new CommandDefinition(sql, new { TokenHash = tokenHash }, cancellationToken: cancellationToken));
        return row?.ToEntity();
    }

    public async Task UpdateAsync(IpChangeConfirmation confirmation, CancellationToken cancellationToken)
    {
        const string sql = """
            update ip_change_confirmations
            set status = @Status,
                confirmed_at = @ConfirmedAt,
                expires_at = @ExpiresAt
            where id = @Id;
            """;

        using var connection = connectionFactory.Create();
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            confirmation.Id,
            Status = confirmation.Status.ToString().ToLowerInvariant(),
            confirmation.ConfirmedAt,
            confirmation.ExpiresAt
        }, cancellationToken: cancellationToken));
    }

    private sealed record Row(int Id, int UserId, int? DeviceId, string RequestedIp, string TokenHash, string Status, DateTimeOffset ExpiresAt, DateTimeOffset CreatedAt, DateTimeOffset? ConfirmedAt)
    {
        public IpChangeConfirmation ToEntity() => new()
        {
            Id = Id,
            UserId = UserId,
            DeviceId = DeviceId,
            RequestedIp = RequestedIp,
            TokenHash = TokenHash,
            Status = Enum.Parse<IpChangeConfirmationStatus>(Status, true),
            ExpiresAt = ExpiresAt,
            CreatedAt = CreatedAt,
            ConfirmedAt = ConfirmedAt
        };
    }
}
