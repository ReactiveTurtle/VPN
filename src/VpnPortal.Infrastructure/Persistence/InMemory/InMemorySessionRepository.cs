using VpnPortal.Application.Interfaces;
using VpnPortal.Domain.Entities;

namespace VpnPortal.Infrastructure.Persistence.InMemory;

public sealed class InMemorySessionRepository(InMemoryPortalStore store) : ISessionRepository
{
    public Task<IReadOnlyCollection<VpnSession>> GetRecentAsync(CancellationToken cancellationToken)
    {
        var sessions = store.Users
            .SelectMany(x => x.Sessions.Select(s => new VpnSession
            {
                Id = s.Id,
                UserId = s.UserId,
                DeviceId = s.DeviceId,
                Device = x.Devices.FirstOrDefault(d => d.Id == s.DeviceId),
                SourceIp = s.SourceIp,
                AssignedVpnIp = s.AssignedVpnIp,
                SessionId = s.SessionId,
                StartedAt = s.StartedAt,
                LastSeenAt = s.LastSeenAt,
                EndedAt = s.EndedAt,
                Active = s.Active,
                Authorized = s.Authorized,
                User = x
            }))
            .OrderByDescending(x => x.StartedAt)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<VpnSession>>(sessions);
    }

    public Task<bool> DisconnectAsync(int sessionId, CancellationToken cancellationToken)
    {
        var session = store.Users.SelectMany(x => x.Sessions).FirstOrDefault(x => x.Id == sessionId);
        if (session is null)
        {
            return Task.FromResult(false);
        }

        session.Active = false;
        session.EndedAt = DateTimeOffset.UtcNow;
        session.Authorized = false;
        return Task.FromResult(true);
    }
}
