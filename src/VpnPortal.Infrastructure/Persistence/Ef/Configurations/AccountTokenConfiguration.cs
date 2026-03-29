using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VpnPortal.Infrastructure.Persistence.Ef.Entities;

namespace VpnPortal.Infrastructure.Persistence.Ef.Configurations;

public sealed class AccountTokenConfiguration : IEntityTypeConfiguration<AccountTokenEntity>
{
    public void Configure(EntityTypeBuilder<AccountTokenEntity> builder)
    {
        builder.ToTable("account_tokens");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.UserEmail).HasMaxLength(255).IsRequired();
        builder.Property(x => x.TokenHash).IsRequired();
        builder.Property(x => x.Purpose).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.Used).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => new { x.UserEmail, x.Purpose }).HasDatabaseName("ix_account_tokens_user_email_purpose");
        builder.HasOne(x => x.CreatedByAdmin).WithMany(x => x.CreatedTokens).HasForeignKey(x => x.CreatedByAdminId).OnDelete(DeleteBehavior.Restrict);
    }
}
