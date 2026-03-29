using VpnPortal.Application.Contracts.Internal;

namespace VpnPortal.Application.Interfaces;

public interface IVpnAccountingService
{
    Task<bool> RecordAsync(VpnAccountingEventCommand command, CancellationToken cancellationToken);
}
