using VpnPortal.Domain.Entities;

namespace VpnPortal.Application.Interfaces;

public interface IDeviceRepository
{
    Task<TrustedDevice?> GetByIdAsync(int deviceId, CancellationToken cancellationToken);
    Task<TrustedDevice> AddAsync(TrustedDevice device, CancellationToken cancellationToken);
    Task<bool> RevokeAsync(int userId, int deviceId, CancellationToken cancellationToken);
}
