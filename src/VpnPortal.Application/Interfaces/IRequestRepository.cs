using VpnPortal.Domain.Entities;

namespace VpnPortal.Application.Interfaces;

public interface IRequestRepository
{
    Task<VpnRequest?> GetLatestPendingByEmailAsync(string email, CancellationToken cancellationToken);
    Task<VpnRequest> AddAsync(VpnRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<VpnRequest>> GetAllAsync(CancellationToken cancellationToken);
    Task<VpnRequest?> GetByIdAsync(int requestId, CancellationToken cancellationToken);
    Task UpdateAsync(VpnRequest request, CancellationToken cancellationToken);
}
