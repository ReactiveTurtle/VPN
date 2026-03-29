using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VpnPortal.Infrastructure.Persistence.Ef.Entities;

namespace VpnPortal.Infrastructure.Persistence.Ef.Configurations;

public sealed class IpChangeConfirmationConfiguration : IEntityTypeConfiguration<IpChangeConfirmationEntity>
{
    public void Configure(EntityTypeBuilder<IpChangeConfirmationEntity> builder)
    {
        builder.ToTable("ip_change_confirmations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.RequestedIp).HasColumnType("inet").IsRequired();
        builder.Property(x => x.TokenHash).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.Status }).HasDatabaseName("ix_ip_change_confirmations_user_id_status");
        builder.HasOne(x => x.User).WithMany(x => x.IpChangeConfirmations).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Device).WithMany(x => x.IpChangeConfirmations).HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.SetNull);
    }
}
