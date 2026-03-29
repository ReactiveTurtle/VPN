using VpnPortal.Application.Interfaces;
using VpnPortal.Domain.Entities;

namespace VpnPortal.Infrastructure.Persistence.InMemory;

public sealed class InMemorySuperAdminRepository(InMemoryPortalStore store) : ISuperAdminRepository
{
    public Task<SuperAdmin?> GetByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        var admin = store.SuperAdmins
            .Where(x => x.Username.Equals(username, StringComparison.OrdinalIgnoreCase))
            .Select(x => new SuperAdmin
            {
                Id = x.Id,
                Username = x.Username,
                PasswordHash = x.PasswordHash,
                CreatedAt = x.CreatedAt,
                LastLoginAt = x.LastLoginAt
            })
            .FirstOrDefault();

        return Task.FromResult(admin);
    }

    public Task UpdateAsync(SuperAdmin superAdmin, CancellationToken cancellationToken)
    {
        var current = store.SuperAdmins.First(x => x.Id == superAdmin.Id);
        current.Username = superAdmin.Username;
        current.PasswordHash = superAdmin.PasswordHash;
        current.CreatedAt = superAdmin.CreatedAt;
        current.LastLoginAt = superAdmin.LastLoginAt;
        return Task.CompletedTask;
    }
}
