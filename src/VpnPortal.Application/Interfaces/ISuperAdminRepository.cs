using VpnPortal.Domain.Entities;

namespace VpnPortal.Application.Interfaces;

public interface ISuperAdminRepository
{
    Task<SuperAdmin?> GetByUsernameAsync(string username, CancellationToken cancellationToken);
    Task UpdateAsync(SuperAdmin superAdmin, CancellationToken cancellationToken);
}
