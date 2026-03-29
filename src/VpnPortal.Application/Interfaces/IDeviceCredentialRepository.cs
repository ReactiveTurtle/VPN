using VpnPortal.Domain.Entities;

namespace VpnPortal.Application.Interfaces;

public interface IDeviceCredentialRepository
{
    Task<VpnDeviceCredential?> GetActiveByDeviceIdAsync(int deviceId, CancellationToken cancellationToken);
    Task<VpnDeviceCredential> AddAsync(VpnDeviceCredential credential, CancellationToken cancellationToken);
    Task UpdateAsync(VpnDeviceCredential credential, CancellationToken cancellationToken);
    Task<bool> RevokeActiveByDeviceIdAsync(int userId, int deviceId, CancellationToken cancellationToken);
}
