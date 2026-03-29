using VpnPortal.Domain.Entities;

namespace VpnPortal.Application.Interfaces;

public interface IUserRepository
{
    Task<IReadOnlyCollection<VpnUser>> GetAllAsync(CancellationToken cancellationToken);
    Task<VpnUser?> GetByIdAsync(int userId, CancellationToken cancellationToken);
    Task<VpnUser?> GetByIdWithRelationsAsync(int userId, CancellationToken cancellationToken);
    Task<VpnUser?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task<VpnUser?> GetByUsernameOrEmailAsync(string login, CancellationToken cancellationToken);
    Task<VpnUser> AddAsync(VpnUser user, CancellationToken cancellationToken);
    Task UpdateAsync(VpnUser user, CancellationToken cancellationToken);
}
