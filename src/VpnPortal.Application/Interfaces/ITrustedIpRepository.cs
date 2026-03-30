using VpnPortal.Domain.Entities;

namespace VpnPortal.Application.Interfaces;

public interface ITrustedIpRepository
{
    Task<IReadOnlyCollection<TrustedIp>> GetByUserIdAsync(int userId, CancellationToken cancellationToken);
    Task<TrustedIp?> GetByUserAndIpAsync(int userId, string ipAddress, CancellationToken cancellationToken);
    Task<TrustedIp?> GetActiveByDeviceIdAsync(int deviceId, CancellationToken cancellationToken);
    Task<TrustedIp> AddAsync(TrustedIp trustedIp, CancellationToken cancellationToken);
    Task UpdateAsync(TrustedIp trustedIp, CancellationToken cancellationToken);
}
