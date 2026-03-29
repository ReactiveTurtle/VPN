using Dapper;
using VpnPortal.Application.Interfaces;
using VpnPortal.Domain.Entities;

namespace VpnPortal.Infrastructure.Persistence.PostgreSql;

public sealed class PostgreSqlSuperAdminRepository(PostgreSqlConnectionFactory connectionFactory) : ISuperAdminRepository
{
    public async Task<SuperAdmin?> GetByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        const string sql = """
            select id,
                   username,
                   password_hash as PasswordHash,
                   created_at as CreatedAt,
                   last_login_at as LastLoginAt
            from superadmins
            where username = @Username;
            """;

        using var connection = connectionFactory.Create();
        return await connection.QuerySingleOrDefaultAsync<SuperAdmin>(new CommandDefinition(sql, new { Username = username }, cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(SuperAdmin superAdmin, CancellationToken cancellationToken)
    {
        const string sql = """
            update superadmins
            set last_login_at = @LastLoginAt,
                password_hash = @PasswordHash
            where id = @Id;
            """;

        using var connection = connectionFactory.Create();
        await connection.ExecuteAsync(new CommandDefinition(sql, new { superAdmin.Id, superAdmin.LastLoginAt, superAdmin.PasswordHash }, cancellationToken: cancellationToken));
    }
}
