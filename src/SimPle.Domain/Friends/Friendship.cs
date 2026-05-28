using SimPle.Domain.Common;

namespace SimPle.Domain.Friends;

public class Friendship : Entity
{
    public Guid RequesterId { get; private set; }
    public Guid AddresseeId { get; private set; }
    public FriendshipStatus Status { get; private set; } = FriendshipStatus.Pending;

    private Friendship() { }

    public static Friendship Request(Guid requesterId, Guid addresseeId) => new()
    {
        RequesterId = requesterId,
        AddresseeId = addresseeId,
        Status = FriendshipStatus.Pending,
    };

    public void Accept() { Status = FriendshipStatus.Accepted; Touch(); }
    public void Decline() { Status = FriendshipStatus.Declined; Touch(); }
}

public class Block : Entity
{
    public Guid BlockerId { get; private set; }
    public Guid BlockedId { get; private set; }

    private Block() { }

    public static Block Create(Guid blockerId, Guid blockedId) => new()
    {
        BlockerId = blockerId,
        BlockedId = blockedId,
    };
}

public enum FriendshipStatus { Pending, Accepted, Declined }
