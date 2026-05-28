using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimPle.Domain.Profiles;
using SimPle.Domain.Users;

namespace SimPle.Infrastructure.Persistence.Configurations;

public sealed class ProfileInterestTagConfiguration : IEntityTypeConfiguration<ProfileInterestTag>
{
    public void Configure(EntityTypeBuilder<ProfileInterestTag> builder)
    {
        builder.ToTable("profile_interest_tags");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).HasMaxLength(50).IsRequired();
        builder.Property(t => t.NormalizedName).HasMaxLength(50).IsRequired();

        builder.HasIndex(t => new { t.UserId, t.NormalizedName }).IsUnique();
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
