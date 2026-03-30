using VpnPortal.Application.Contracts.System;

namespace VpnPortal.Application.Interfaces;

public interface ISystemStatusService
{
    bool IsDatabaseConfigured { get; }
    Task<DatabaseStatusDto> GetDatabaseStatusAsync(CancellationToken cancellationToken);
}
