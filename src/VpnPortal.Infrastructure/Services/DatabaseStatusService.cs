using VpnPortal.Application.Contracts.System;
using VpnPortal.Application.Interfaces;
using Microsoft.Extensions.Options;
using Npgsql;
using VpnPortal.Infrastructure.Options;

namespace VpnPortal.Infrastructure.Services;

public sealed class DatabaseStatusService(IOptions<DatabaseOptions> options) : ISystemStatusService
{
    public bool IsDatabaseConfigured => !string.IsNullOrWhiteSpace(options.Value.ConnectionString);

    public async Task<DatabaseStatusDto> GetDatabaseStatusAsync(CancellationToken cancellationToken)
    {
        if (!IsDatabaseConfigured)
        {
            return new DatabaseStatusDto(false, false, null);
        }

        try
        {
            await using var connection = new NpgsqlConnection(options.Value.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand("select 1", connection);
            await command.ExecuteScalarAsync(cancellationToken);
            return new DatabaseStatusDto(true, true, null);
        }
        catch (Exception exception)
        {
            return new DatabaseStatusDto(true, false, exception.Message);
        }
    }
}
