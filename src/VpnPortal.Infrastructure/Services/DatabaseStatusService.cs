using Microsoft.Extensions.Options;
using Npgsql;
using VpnPortal.Infrastructure.Options;

namespace VpnPortal.Infrastructure.Services;

public sealed class DatabaseStatusService(IOptions<DatabaseOptions> options)
{
    public bool IsConfigured => !string.IsNullOrWhiteSpace(options.Value.ConnectionString);

    public async Task<bool> CanConnectAsync(CancellationToken cancellationToken)
    {
        if (!string.Equals(options.Value.Provider, "PostgreSql", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!IsConfigured)
        {
            return false;
        }

        await using var connection = new NpgsqlConnection(options.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("select 1", connection);
        await command.ExecuteScalarAsync(cancellationToken);
        return true;
    }
}
