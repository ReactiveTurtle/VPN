using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VpnPortal.Infrastructure.Persistence.Ef.Entities;

namespace VpnPortal.Infrastructure.Persistence.Ef.Configurations;

public sealed class TrustedIpConfiguration : IEntityTypeConfiguration<TrustedIpEntity>
{
    public void Configure(EntityTypeBuilder<TrustedIpEntity> builder)
    {
        builder.ToTable("trusted_ips");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.IpAddress).HasColumnType("inet").IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.FirstSeenAt).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.Status }).HasDatabaseName("ix_trusted_ips_user_id_status");
        builder.HasOne(x => x.User).WithMany(x => x.TrustedIps).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Device).WithMany(x => x.TrustedIps).HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.SetNull);
    }
}
