using VpnPortal.Application.Contracts.Admin;

namespace VpnPortal.Application.Interfaces;

public interface IAdminOperationsService
{
    Task<IReadOnlyCollection<AdminUserDto>> GetUsersAsync(CancellationToken cancellationToken);
    Task<AdminUserDto?> UpdateUserAsync(int userId, int maxDevices, long? actorId, string? ipAddress, CancellationToken cancellationToken);
    Task<AdminUserDto?> SetUserActiveAsync(int userId, bool active, long? actorId, string? ipAddress, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AdminSessionDto>> GetSessionsAsync(CancellationToken cancellationToken);
    Task<bool> DisconnectSessionAsync(int sessionId, long? actorId, string? ipAddress, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AuditLogDto>> GetAuditLogAsync(CancellationToken cancellationToken);
}
