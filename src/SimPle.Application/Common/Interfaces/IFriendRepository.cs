using SimPle.Domain.Friends;
using SimPle.Domain.Users;

namespace SimPle.Application.Common.Interfaces;

public interface IFriendRepository
{
    Task<Friendship?> GetFriendshipAsync(Guid userId, Guid friendUserId, CancellationToken ct = default);
    Task<bool> AreFriendsAsync(Guid userId, Guid friendUserId, CancellationToken ct = default);
    Task<int> CountFriendsAsync(Guid userId, CancellationToken ct = default);
    Task<int> CountMutualFriendsAsync(Guid userId, Guid otherUserId, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetFriendsAsync(Guid userId, CancellationToken ct = default);

    Task<FriendRequest?> GetRequestAsync(Guid requestId, CancellationToken ct = default);
    Task<FriendRequest?> GetPendingRequestBetweenAsync(Guid firstUserId, Guid secondUserId, CancellationToken ct = default);
    Task<IReadOnlyList<FriendRequest>> GetIncomingRequestsAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<FriendRequest>> GetOutgoingRequestsAsync(Guid userId, CancellationToken ct = default);
    Task AddRequestAsync(FriendRequest request, CancellationToken ct = default);
    Task UpdateRequestAsync(FriendRequest request, CancellationToken ct = default);

    Task AddFriendshipAsync(Friendship friendship, CancellationToken ct = default);
    Task RemoveFriendshipAsync(Guid userId, Guid friendUserId, CancellationToken ct = default);

    Task<UserBlock?> GetBlockAsync(Guid blockerUserId, Guid blockedUserId, CancellationToken ct = default);
    Task<bool> IsBlockedEitherDirectionAsync(Guid firstUserId, Guid secondUserId, CancellationToken ct = default);
    Task<IReadOnlyList<UserBlock>> GetBlocksAsync(Guid blockerUserId, CancellationToken ct = default);
    Task AddBlockAsync(UserBlock block, CancellationToken ct = default);
    Task RemoveBlockAsync(Guid blockerUserId, Guid blockedUserId, CancellationToken ct = default);
    Task ClearRelationshipAsync(Guid firstUserId, Guid secondUserId, CancellationToken ct = default);

    Task<IReadOnlyList<User>> SearchUsersAsync(Guid requesterId, string query, int limit, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetSuggestionsAsync(Guid requesterId, int limit, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
