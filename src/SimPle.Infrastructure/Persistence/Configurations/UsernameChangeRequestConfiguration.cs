using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimPle.Domain.Profiles;
using SimPle.Domain.Users;

namespace SimPle.Infrastructure.Persistence.Configurations;

public sealed class UsernameChangeRequestConfiguration : IEntityTypeConfiguration<UsernameChangeRequest>
{
    public void Configure(EntityTypeBuilder<UsernameChangeRequest> builder)
    {
        builder.ToTable("username_change_requests");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.RequestedUsername).HasMaxLength(30).IsRequired();
        builder.Property(r => r.NormalizedRequestedUsername).HasMaxLength(30).IsRequired();
        builder.Property(r => r.Status).HasConversion<string>().IsRequired();
        builder.Property(r => r.RejectionReason).HasMaxLength(256);

        builder.HasIndex(r => r.UserId);
        builder.HasIndex(r => new { r.UserId, r.Status });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
