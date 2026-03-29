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

    public Task RecordAuthorizedAsync(VpnSession session, CancellationToken cancellationToken)
    {
        var user = store.Users.First(x => x.Id == session.UserId);
        var existing = user.Sessions.FirstOrDefault(x => string.Equals(x.SessionId, session.SessionId, StringComparison.Ordinal));
        if (existing is null)
        {
            user.Sessions.Add(new VpnSession
            {
                Id = store.AllocateSessionId(),
                UserId = session.UserId,
                DeviceId = session.DeviceId,
                SourceIp = session.SourceIp,
                AssignedVpnIp = session.AssignedVpnIp,
                NasIdentifier = session.NasIdentifier,
                SessionId = session.SessionId,
                StartedAt = session.StartedAt,
                LastSeenAt = session.LastSeenAt,
                Active = session.Active,
                Authorized = session.Authorized
            });
        }
        else
        {
            existing.SourceIp = session.SourceIp;
            existing.AssignedVpnIp = session.AssignedVpnIp;
            existing.NasIdentifier = session.NasIdentifier;
            existing.LastSeenAt = session.LastSeenAt;
            existing.Active = session.Active;
            existing.Authorized = session.Authorized;
        }

        return Task.CompletedTask;
    }

    public Task<bool> CloseBySessionIdAsync(int userId, string sessionId, DateTimeOffset endedAt, string? terminationReason, CancellationToken cancellationToken)
    {
        var session = store.Users
            .FirstOrDefault(x => x.Id == userId)?
            .Sessions.FirstOrDefault(x => string.Equals(x.SessionId, sessionId, StringComparison.Ordinal) && x.Active);

        if (session is null)
        {
            return Task.FromResult(false);
        }

        session.Active = false;
        session.EndedAt = endedAt;
        session.LastSeenAt = endedAt;
        session.TerminationReason = terminationReason;
        return Task.FromResult(true);
    }
}
