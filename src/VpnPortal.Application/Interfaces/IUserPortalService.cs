using VpnPortal.Application.Contracts.Users;

namespace VpnPortal.Application.Interfaces;

public interface IUserPortalService
{
    Task<UserDashboardDto?> GetDashboardAsync(int userId, CancellationToken cancellationToken);
    Task<IssuedVpnDeviceCredentialDto?> IssueDeviceCredentialAsync(int userId, IssueVpnDeviceCredentialCommand command, CancellationToken cancellationToken);
    Task<IssuedVpnDeviceCredentialDto?> RotateDeviceCredentialAsync(int userId, int deviceId, CancellationToken cancellationToken);
    Task<bool> RevokeDeviceAsync(int userId, int deviceId, CancellationToken cancellationToken);
    Task<bool> UnbindDeviceIpAsync(int userId, int deviceId, CancellationToken cancellationToken);
}
