using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VpnPortal.Infrastructure.Persistence.Ef.Entities;

namespace VpnPortal.Infrastructure.Persistence.Ef.Configurations;

public sealed class TrustedDeviceConfiguration : IEntityTypeConfiguration<TrustedDeviceEntity>
{
    public void Configure(EntityTypeBuilder<TrustedDeviceEntity> builder)
    {
        builder.ToTable("trusted_devices");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.DeviceUuid).HasMaxLength(128).IsRequired();
        builder.Property(x => x.DeviceName).HasMaxLength(255);
        builder.Property(x => x.DeviceType).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Platform).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.FirstSeenAt).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.DeviceUuid }).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.Status }).HasDatabaseName("ix_trusted_devices_user_id_status");
        builder.HasOne(x => x.User).WithMany(x => x.Devices).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
