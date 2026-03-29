using VpnPortal.Domain.Entities;

namespace VpnPortal.Application.Interfaces;

public interface IIpChangeConfirmationRepository
{
    Task<IReadOnlyCollection<IpChangeConfirmation>> GetPendingByUserIdAsync(int userId, CancellationToken cancellationToken);
    Task<IpChangeConfirmation> AddAsync(IpChangeConfirmation confirmation, CancellationToken cancellationToken);
    Task<IpChangeConfirmation?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken);
    Task UpdateAsync(IpChangeConfirmation confirmation, CancellationToken cancellationToken);
}
