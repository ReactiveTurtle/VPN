using Microsoft.EntityFrameworkCore;
using VpnPortal.Infrastructure.Persistence.Ef.Entities;

namespace VpnPortal.Infrastructure.Persistence.Ef;

public sealed class VpnPortalDbContext(DbContextOptions<VpnPortalDbContext> options) : DbContext(options)
{
    public DbSet<VpnUserEntity> VpnUsers => Set<VpnUserEntity>();
    public DbSet<SuperAdminEntity> SuperAdmins => Set<SuperAdminEntity>();
    public DbSet<VpnRequestEntity> VpnRequests => Set<VpnRequestEntity>();
    public DbSet<AccountTokenEntity> AccountTokens => Set<AccountTokenEntity>();
    public DbSet<TrustedDeviceEntity> TrustedDevices => Set<TrustedDeviceEntity>();
    public DbSet<VpnDeviceCredentialEntity> VpnDeviceCredentials => Set<VpnDeviceCredentialEntity>();
    public DbSet<TrustedIpEntity> TrustedIps => Set<TrustedIpEntity>();
    public DbSet<VpnSessionEntity> VpnSessions => Set<VpnSessionEntity>();
    public DbSet<IpChangeConfirmationEntity> IpChangeConfirmations => Set<IpChangeConfirmationEntity>();
    public DbSet<AuditLogEntity> AuditLogs => Set<AuditLogEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VpnPortalDbContext).Assembly);
    }
}
