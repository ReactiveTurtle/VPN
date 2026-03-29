using Microsoft.EntityFrameworkCore;
using VpnPortal.Application.Interfaces;
using VpnPortal.Domain.Entities;
using VpnPortal.Infrastructure.Persistence.Ef.Entities;
using VpnPortal.Infrastructure.Persistence.Ef.Mappers;

namespace VpnPortal.Infrastructure.Persistence.Ef.Repositories;

public sealed class EfSuperAdminRepository(VpnPortalDbContext dbContext) : ISuperAdminRepository
{
    public async Task<SuperAdmin?> GetByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        var entity = await dbContext.SuperAdmins
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Username == username, cancellationToken);

        return entity?.ToDomain();
    }

    public async Task UpdateAsync(SuperAdmin superAdmin, CancellationToken cancellationToken)
    {
        var entity = await dbContext.SuperAdmins.SingleAsync(x => x.Id == superAdmin.Id, cancellationToken);
        entity.ApplyFromDomain(superAdmin);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
