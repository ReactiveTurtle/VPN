using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VpnPortal.Infrastructure.Persistence.Ef.Entities;

namespace VpnPortal.Infrastructure.Persistence.Ef.Configurations;

public sealed class VpnDeviceCredentialConfiguration : IEntityTypeConfiguration<VpnDeviceCredentialEntity>
{
    public void Configure(EntityTypeBuilder<VpnDeviceCredentialEntity> builder)
    {
        builder.ToTable("vpn_device_credentials");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.VpnUsername).HasMaxLength(128).IsRequired();
        builder.Property(x => x.PasswordHash).IsRequired();
        builder.Property(x => x.RadiusNtHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => x.VpnUsername).IsUnique();
        builder.HasIndex(x => x.DeviceId).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.Status }).HasDatabaseName("ix_vpn_device_credentials_user_id_status");
        builder.HasOne(x => x.User).WithMany(x => x.DeviceCredentials).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Device).WithOne(x => x.Credential).HasForeignKey<VpnDeviceCredentialEntity>(x => x.DeviceId).OnDelete(DeleteBehavior.Cascade);
    }
}
