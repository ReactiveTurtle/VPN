using Dapper;
using VpnPortal.Application.Interfaces;
using VpnPortal.Domain.Entities;
using VpnPortal.Domain.Enums;

namespace VpnPortal.Infrastructure.Persistence.PostgreSql;

public sealed class PostgreSqlAccountTokenRepository(PostgreSqlConnectionFactory connectionFactory) : IAccountTokenRepository
{
    public async Task<AccountToken> AddAsync(AccountToken token, CancellationToken cancellationToken)
    {
        const string sql = """
            insert into account_tokens (user_email, token_hash, purpose, expires_at, used, used_at, created_at)
            values (@UserEmail, @TokenHash, @Purpose, @ExpiresAt, @Used, @UsedAt, @CreatedAt)
            returning id, user_email as UserEmail, token_hash as TokenHash, purpose, expires_at as ExpiresAt, used, used_at as UsedAt, created_at as CreatedAt;
            """;

        using var connection = connectionFactory.Create();
        var row = await connection.QuerySingleAsync<TokenRow>(new CommandDefinition(sql, new
        {
            token.UserEmail,
            token.TokenHash,
            Purpose = ToStoragePurpose(token.Purpose),
            token.ExpiresAt,
            token.Used,
            token.UsedAt,
            token.CreatedAt
        }, cancellationToken: cancellationToken));

        return row.ToEntity();
    }

    public async Task<AccountToken?> GetByHashAsync(string tokenHash, AccountTokenPurpose purpose, CancellationToken cancellationToken)
    {
        const string sql = """
            select id, user_email as UserEmail, token_hash as TokenHash, purpose, expires_at as ExpiresAt, used, used_at as UsedAt, created_at as CreatedAt
            from account_tokens
            where token_hash = @TokenHash and purpose = @Purpose
            order by created_at desc
            limit 1;
            """;

        using var connection = connectionFactory.Create();
        var row = await connection.QuerySingleOrDefaultAsync<TokenRow>(new CommandDefinition(sql, new { TokenHash = tokenHash, Purpose = ToStoragePurpose(purpose) }, cancellationToken: cancellationToken));
        return row?.ToEntity();
    }

    public async Task<AccountToken?> GetLatestByEmailAsync(string email, AccountTokenPurpose purpose, CancellationToken cancellationToken)
    {
        const string sql = """
            select id, user_email as UserEmail, token_hash as TokenHash, purpose, expires_at as ExpiresAt, used, used_at as UsedAt, created_at as CreatedAt
            from account_tokens
            where user_email = @Email and purpose = @Purpose
            order by created_at desc
            limit 1;
            """;

        using var connection = connectionFactory.Create();
        var row = await connection.QuerySingleOrDefaultAsync<TokenRow>(new CommandDefinition(sql, new { Email = email, Purpose = ToStoragePurpose(purpose) }, cancellationToken: cancellationToken));
        return row?.ToEntity();
    }

    public async Task UpdateAsync(AccountToken token, CancellationToken cancellationToken)
    {
        const string sql = """
            update account_tokens
            set used = @Used,
                used_at = @UsedAt,
                expires_at = @ExpiresAt
            where id = @Id;
            """;

        using var connection = connectionFactory.Create();
        await connection.ExecuteAsync(new CommandDefinition(sql, new { token.Id, token.Used, token.UsedAt, token.ExpiresAt }, cancellationToken: cancellationToken));
    }

    private static string ToStoragePurpose(AccountTokenPurpose purpose) => purpose switch
    {
        AccountTokenPurpose.AccountActivation => "account_activation",
        AccountTokenPurpose.IpConfirmation => "ip_confirmation",
        AccountTokenPurpose.PasswordReset => "password_reset",
        _ => throw new ArgumentOutOfRangeException(nameof(purpose), purpose, null)
    };

    private sealed record TokenRow(int Id, string UserEmail, string TokenHash, string Purpose, DateTimeOffset ExpiresAt, bool Used, DateTimeOffset? UsedAt, DateTimeOffset CreatedAt)
    {
        public AccountToken ToEntity()
        {
            return new AccountToken
            {
                Id = Id,
                UserEmail = UserEmail,
                TokenHash = TokenHash,
                Purpose = Purpose switch
                {
                    "account_activation" => AccountTokenPurpose.AccountActivation,
                    "ip_confirmation" => AccountTokenPurpose.IpConfirmation,
                    "password_reset" => AccountTokenPurpose.PasswordReset,
                    _ => throw new InvalidOperationException($"Unsupported token purpose '{Purpose}'.")
                },
                ExpiresAt = ExpiresAt,
                Used = Used,
                UsedAt = UsedAt,
                CreatedAt = CreatedAt
            };
        }
    }
}
