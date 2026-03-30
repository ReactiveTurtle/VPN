using VpnPortal.Application.Contracts.Admin;
using VpnPortal.Application.Interfaces;
using VpnPortal.Domain.Entities;

namespace VpnPortal.Application.Services;

public sealed class AdminOperationsService(IUserRepository userRepository, ISessionRepository sessionRepository, IAuditLogRepository auditLogRepository, IAuditService auditService, IVpnRuntimeControlService vpnRuntimeControlService) : IAdminOperationsService
{
    public async Task<IReadOnlyCollection<AdminUserDto>> GetUsersAsync(CancellationToken cancellationToken)
    {
        var users = await userRepository.GetAllAsync(cancellationToken);
        return users
            .OrderByDescending(x => x.CreatedAt)
            .Select(MapUser)
            .ToArray();
    }

    public async Task<AdminUserDto?> UpdateUserAsync(int userId, int maxDevices, long? actorId, string? ipAddress, CancellationToken cancellationToken)
    {
        if (maxDevices < 1)
        {
            return null;
        }

        var user = await userRepository.GetByIdWithRelationsAsync(userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        user.SetMaxDevices(maxDevices);
        await userRepository.UpdateAsync(user, cancellationToken);
        await auditService.WriteAsync("superadmin", actorId, "user_max_devices_changed", "vpn_user", userId.ToString(), ipAddress, new { maxDevices }, cancellationToken);
        return MapUser(user);
    }

    public async Task<AdminUserDto?> SetUserActiveAsync(int userId, bool active, long? actorId, string? ipAddress, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdWithRelationsAsync(userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        user.SetActive(active);
        await userRepository.UpdateAsync(user, cancellationToken);
        await auditService.WriteAsync("superadmin", actorId, active ? "user_activated" : "user_deactivated", "vpn_user", userId.ToString(), ipAddress, new { active }, cancellationToken);
        return MapUser(user);
    }

    public async Task<IReadOnlyCollection<AdminSessionDto>> GetSessionsAsync(CancellationToken cancellationToken)
    {
        var sessions = await sessionRepository.GetRecentAsync(cancellationToken);
        return sessions.Select(x => new AdminSessionDto(x.Id, x.UserId, x.User?.Username ?? $"user-{x.UserId}", x.Device?.DeviceName, x.SourceIp, x.AssignedVpnIp, x.StartedAt, x.LastSeenAt, x.Active, x.Authorized)).ToArray();
    }

    public async Task<bool> DisconnectSessionAsync(int sessionId, long? actorId, string? ipAddress, CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return false;
        }

        var runtimeDisconnectRequested = await vpnRuntimeControlService.RequestDisconnectAsync(session, cancellationToken);
        var disconnected = await sessionRepository.DisconnectAsync(sessionId, cancellationToken);
        if (disconnected)
        {
            await auditService.WriteAsync("superadmin", actorId, "session_disconnected", "vpn_session", sessionId.ToString(), ipAddress, new { sessionId, runtimeDisconnectRequested }, cancellationToken);
        }

        return disconnected;
    }

    public async Task<IReadOnlyCollection<AuditLogDto>> GetAuditLogAsync(CancellationToken cancellationToken)
    {
        var entries = await auditLogRepository.GetRecentAsync(100, cancellationToken);
        return entries.Select(x => new AuditLogDto(x.Id, x.ActorType, x.ActorId, x.Action, x.EntityType, x.EntityId, x.IpAddress, x.DetailsJson, x.CreatedAt)).ToArray();
    }

    private static AdminUserDto MapUser(VpnUser user)
    {
        return new AdminUserDto(user.Id, user.Email, user.Username, user.Active, user.EmailConfirmed, user.MaxDevices, user.Devices.Count, user.CreatedAt, user.LastLoginAt);
    }
}
