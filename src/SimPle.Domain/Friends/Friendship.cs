using SimPle.Domain.Common;

namespace SimPle.Domain.Friends;

public sealed class Friendship : Entity
{
    public Guid UserId { get; private set; }
    public Guid FriendUserId { get; private set; }

    private Friendship() { }

    public static Friendship Create(Guid userId, Guid friendUserId)
    {
        if (userId == friendUserId)
            throw new ArgumentException("A user cannot be friends with themselves.");

        return new Friendship
        {
            UserId = userId,
            FriendUserId = friendUserId
        };
    }
}

public sealed class FriendRequest : Entity
{
    public Guid SenderUserId { get; private set; }
    public Guid ReceiverUserId { get; private set; }
    public FriendRequestStatus Status { get; private set; } = FriendRequestStatus.Pending;
    public DateTime? RespondedAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }

    private FriendRequest() { }

    public static FriendRequest Create(Guid senderUserId, Guid receiverUserId)
    {
        if (senderUserId == receiverUserId)
            throw new ArgumentException("A user cannot send a friend request to themselves.");

        return new FriendRequest
        {
            SenderUserId = senderUserId,
            ReceiverUserId = receiverUserId
        };
    }

    public void Accept()
    {
        if (Status != FriendRequestStatus.Pending) return;
        Status = FriendRequestStatus.Accepted;
        RespondedAtUtc = DateTime.UtcNow;
        Touch();
    }

    public void Decline()
    {
        if (Status != FriendRequestStatus.Pending) return;
        Status = FriendRequestStatus.Declined;
        RespondedAtUtc = DateTime.UtcNow;
        Touch();
    }

    public void Cancel()
    {
        if (Status != FriendRequestStatus.Pending) return;
        Status = FriendRequestStatus.Cancelled;
        CancelledAtUtc = DateTime.UtcNow;
        Touch();
    }
}

public sealed class UserBlock : Entity
{
    public Guid BlockerUserId { get; private set; }
    public Guid BlockedUserId { get; private set; }

    private UserBlock() { }

    public static UserBlock Create(Guid blockerUserId, Guid blockedUserId)
    {
        if (blockerUserId == blockedUserId)
            throw new ArgumentException("A user cannot block themselves.");

        return new UserBlock
        {
            BlockerUserId = blockerUserId,
            BlockedUserId = blockedUserId
        };
    }
}

public enum FriendRequestStatus
{
    Pending,
    Accepted,
    Declined,
    Cancelled
}

public enum FriendRequestPolicy
{
    Anyone,
    FriendsOfFriends,
    Off
}
