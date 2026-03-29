using VpnPortal.Domain.Entities;

namespace VpnPortal.Application.Interfaces;

public interface ISessionRepository
{
    Task<IReadOnlyCollection<VpnSession>> GetRecentAsync(CancellationToken cancellationToken);
    Task<bool> DisconnectAsync(int sessionId, CancellationToken cancellationToken);
}
