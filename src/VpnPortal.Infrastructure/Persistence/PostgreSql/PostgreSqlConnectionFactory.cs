using System.Data;
using Npgsql;
using VpnPortal.Infrastructure.Options;

namespace VpnPortal.Infrastructure.Persistence.PostgreSql;

public sealed class PostgreSqlConnectionFactory(DatabaseOptions options)
{
    public IDbConnection Create()
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            throw new InvalidOperationException("Database connection string is not configured.");
        }

        return new NpgsqlConnection(options.ConnectionString);
    }
}
