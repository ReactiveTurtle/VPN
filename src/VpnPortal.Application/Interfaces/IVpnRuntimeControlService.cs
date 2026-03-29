using VpnPortal.Domain.Entities;

namespace VpnPortal.Application.Interfaces;

public interface IVpnRuntimeControlService
{
    Task<bool> RequestDisconnectAsync(VpnSession session, CancellationToken cancellationToken);
}
