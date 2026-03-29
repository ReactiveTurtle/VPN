using VpnPortal.Application.Interfaces;
using VpnPortal.Domain.Entities;
using VpnPortal.Domain.Enums;

namespace VpnPortal.Infrastructure.Persistence.InMemory;

public sealed class InMemoryDeviceRepository(InMemoryPortalStore store) : IDeviceRepository
{
    public Task<TrustedDevice?> GetByIdAsync(int deviceId, CancellationToken cancellationToken)
    {
        var device = store.Users
            .SelectMany(x => x.Devices)
            .FirstOrDefault(x => x.Id == deviceId);

        return Task.FromResult(device is null ? null : new TrustedDevice
        {
            Id = device.Id,
            UserId = device.UserId,
            DeviceUuid = device.DeviceUuid,
            DeviceName = device.DeviceName,
            DeviceType = device.DeviceType,
            Platform = device.Platform,
            Status = device.Status,
            FirstSeenAt = device.FirstSeenAt,
            LastSeenAt = device.LastSeenAt
        });
    }

    public Task<bool> RevokeAsync(int userId, int deviceId, CancellationToken cancellationToken)
    {
        var device = store.Users
            .FirstOrDefault(x => x.Id == userId)?
            .Devices.FirstOrDefault(x => x.Id == deviceId);

        if (device is null)
        {
            return Task.FromResult(false);
        }

        device.Status = DeviceStatus.Revoked;
        return Task.FromResult(true);
    }
}
