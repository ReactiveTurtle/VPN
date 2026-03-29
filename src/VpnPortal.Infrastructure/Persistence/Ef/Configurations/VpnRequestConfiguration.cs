using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VpnPortal.Infrastructure.Persistence.Ef.Entities;

namespace VpnPortal.Infrastructure.Persistence.Ef.Configurations;

public sealed class VpnRequestConfiguration : IEntityTypeConfiguration<VpnRequestEntity>
{
    public void Configure(EntityTypeBuilder<VpnRequestEntity> builder)
    {
        builder.ToTable("vpn_requests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Email).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(255);
        builder.Property(x => x.RequestedByIp).HasColumnType("inet");
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.SubmittedAt).IsRequired();
        builder.Property(x => x.AdminComment);
        builder.HasIndex(x => x.Email).HasDatabaseName("ix_vpn_requests_email");
        builder.HasIndex(x => new { x.Status, x.SubmittedAt }).HasDatabaseName("ix_vpn_requests_status_submitted_at");
        builder.HasOne(x => x.ProcessedByAdmin).WithMany(x => x.ProcessedRequests).HasForeignKey(x => x.ProcessedByAdminId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ApprovedUser).WithMany(x => x.ApprovedRequests).HasForeignKey(x => x.ApprovedUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
