using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimPle.Domain.Profiles;
using SimPle.Domain.Users;

namespace SimPle.Infrastructure.Persistence.Configurations;

public sealed class ProfileExternalLinkConfiguration : IEntityTypeConfiguration<ProfileExternalLink>
{
    public void Configure(EntityTypeBuilder<ProfileExternalLink> builder)
    {
        builder.ToTable("profile_external_links");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Platform).HasMaxLength(32).IsRequired();
        builder.Property(l => l.Url).HasMaxLength(512).IsRequired();
        builder.Property(l => l.DisplayLabel).HasMaxLength(64);

        builder.HasIndex(l => l.UserId);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
