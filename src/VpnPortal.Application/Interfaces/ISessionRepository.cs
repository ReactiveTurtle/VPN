using VpnPortal.Domain.Entities;

namespace VpnPortal.Application.Interfaces;

public interface ISessionRepository
{
    Task<IReadOnlyCollection<VpnSession>> GetRecentAsync(CancellationToken cancellationToken);
    Task RecordAuthorizedAsync(VpnSession session, CancellationToken cancellationToken);
    Task<bool> CloseBySessionIdAsync(int userId, string sessionId, DateTimeOffset endedAt, string? terminationReason, CancellationToken cancellationToken);
    Task<bool> DisconnectAsync(int sessionId, CancellationToken cancellationToken);
}
