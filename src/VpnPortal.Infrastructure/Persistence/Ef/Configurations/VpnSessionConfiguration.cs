using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VpnPortal.Infrastructure.Persistence.Ef.Entities;

namespace VpnPortal.Infrastructure.Persistence.Ef.Configurations;

public sealed class VpnSessionConfiguration : IEntityTypeConfiguration<VpnSessionEntity>
{
    public void Configure(EntityTypeBuilder<VpnSessionEntity> builder)
    {
        builder.ToTable("vpn_sessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.SourceIp).HasColumnType("inet").IsRequired();
        builder.Property(x => x.AssignedVpnIp).HasColumnType("inet");
        builder.Property(x => x.NasIdentifier).HasMaxLength(128);
        builder.Property(x => x.SessionId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.StartedAt).IsRequired();
        builder.Property(x => x.TerminationReason).HasMaxLength(64);
        builder.Property(x => x.Active).IsRequired();
        builder.Property(x => x.Authorized).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.StartedAt }).HasDatabaseName("ix_vpn_sessions_user_id_started_at");
        builder.HasIndex(x => x.Active).HasDatabaseName("ix_vpn_sessions_active");
        builder.HasIndex(x => x.SessionId).IsUnique().HasDatabaseName("ux_vpn_sessions_session_id");
        builder.HasOne(x => x.User).WithMany(x => x.Sessions).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Device).WithMany(x => x.Sessions).HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.SetNull);
    }
}
