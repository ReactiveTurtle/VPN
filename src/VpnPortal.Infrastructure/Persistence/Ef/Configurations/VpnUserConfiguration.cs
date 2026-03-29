using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VpnPortal.Infrastructure.Persistence.Ef.Entities;

namespace VpnPortal.Infrastructure.Persistence.Ef.Configurations;

public sealed class VpnUserConfiguration : IEntityTypeConfiguration<VpnUserEntity>
{
    public void Configure(EntityTypeBuilder<VpnUserEntity> builder)
    {
        builder.ToTable("vpn_users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Email).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Username).HasMaxLength(64).IsRequired();
        builder.Property(x => x.PasswordHash).IsRequired();
        builder.Property(x => x.MaxDevices).IsRequired();
        builder.Property(x => x.Active).IsRequired();
        builder.Property(x => x.EmailConfirmed).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.HasIndex(x => x.Email).IsUnique();
        builder.HasIndex(x => x.Username).IsUnique();
    }
}
