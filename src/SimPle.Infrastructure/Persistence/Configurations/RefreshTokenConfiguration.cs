using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimPle.Domain.Users;

namespace SimPle.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TokenHash).HasMaxLength(100).IsRequired();
        builder.Property(t => t.CreatedByIp).HasMaxLength(45).IsRequired(); // 45 chars covers IPv6
        builder.Property(t => t.UserAgent).HasMaxLength(512);
        builder.Property(t => t.RevokedReason).HasMaxLength(100);
        builder.Property(t => t.ReplacedByTokenHash).HasMaxLength(100);

        // Looking up a token by hash is the hot path during refresh and logout.
        builder.HasIndex(t => t.TokenHash).IsUnique();
        // Needed when revoking an entire session family.
        builder.HasIndex(t => t.FamilyId);
        // Needed for logout-all.
        builder.HasIndex(t => t.UserId);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // PostgreSQL xmin is a per-row system column that changes on every update.
        // EF Core uses it as an optimistic concurrency token at no migration cost.
        // Concurrent refresh requests that both read the same token will race here;
        // the loser receives DbUpdateConcurrencyException and is treated as a replay.
        builder.UseXminAsConcurrencyToken();
    }
}
