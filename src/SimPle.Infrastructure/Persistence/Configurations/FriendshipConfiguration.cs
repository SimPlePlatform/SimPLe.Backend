using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimPle.Domain.Friends;

namespace SimPle.Infrastructure.Persistence.Configurations;

public sealed class FriendshipConfiguration : IEntityTypeConfiguration<Friendship>
{
    public void Configure(EntityTypeBuilder<Friendship> builder)
    {
        builder.ToTable("friendships");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.UserId).IsRequired();
        builder.Property(f => f.FriendUserId).IsRequired();

        builder.HasIndex(f => new { f.UserId, f.FriendUserId }).IsUnique();
        builder.HasIndex(f => f.FriendUserId);
    }
}

public sealed class FriendRequestConfiguration : IEntityTypeConfiguration<FriendRequest>
{
    public void Configure(EntityTypeBuilder<FriendRequest> builder)
    {
        builder.ToTable("friend_requests");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.SenderUserId).IsRequired();
        builder.Property(r => r.ReceiverUserId).IsRequired();
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasIndex(r => new { r.SenderUserId, r.ReceiverUserId, r.Status });
        builder.HasIndex(r => new { r.ReceiverUserId, r.Status });
    }
}

public sealed class UserBlockConfiguration : IEntityTypeConfiguration<UserBlock>
{
    public void Configure(EntityTypeBuilder<UserBlock> builder)
    {
        builder.ToTable("user_blocks");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.BlockerUserId).IsRequired();
        builder.Property(b => b.BlockedUserId).IsRequired();

        builder.HasIndex(b => new { b.BlockerUserId, b.BlockedUserId }).IsUnique();
        builder.HasIndex(b => b.BlockedUserId);
    }
}
