using VpnPortal.Application.Contracts.Internal;

namespace VpnPortal.Application.Interfaces;

public interface IVpnAuthEventService
{
    Task<bool> RecordAsync(VpnAuthEventCommand command, CancellationToken cancellationToken);
}
