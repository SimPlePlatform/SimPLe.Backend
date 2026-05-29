using SimPle.Application.Friends.DTOs;
using SimPle.Shared.Common;

namespace SimPle.Application.Friends.Services;

public interface IFriendService
{
    Task<Result<IReadOnlyList<UserSummaryDto>>> GetFriendsAsync(Guid userId, CancellationToken ct = default);
    Task<Result<FriendRequestsDto>> GetRequestsAsync(Guid userId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<FriendRequestDto>>> GetIncomingRequestsAsync(Guid userId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<FriendRequestDto>>> GetOutgoingRequestsAsync(Guid userId, CancellationToken ct = default);
    Task<Result<FriendRequestDto>> SendRequestAsync(Guid senderUserId, Guid receiverUserId, CancellationToken ct = default);
    Task<Result<FriendRequestDto>> AcceptRequestAsync(Guid userId, Guid requestId, CancellationToken ct = default);
    Task<Result<FriendRequestDto>> DeclineRequestAsync(Guid userId, Guid requestId, CancellationToken ct = default);
    Task<Result<FriendRequestDto>> CancelRequestAsync(Guid userId, Guid requestId, CancellationToken ct = default);
    Task<Result> RemoveFriendAsync(Guid userId, Guid friendUserId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<UserSummaryDto>>> SearchAsync(Guid userId, string query, CancellationToken ct = default);
    Task<Result<IReadOnlyList<UserSummaryDto>>> SuggestionsAsync(Guid userId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<BlockedUserDto>>> GetBlocksAsync(Guid userId, CancellationToken ct = default);
    Task<Result<BlockedUserDto>> BlockAsync(Guid blockerUserId, Guid blockedUserId, CancellationToken ct = default);
    Task<Result> UnblockAsync(Guid blockerUserId, Guid blockedUserId, CancellationToken ct = default);
    Task<Result<FriendPrivacyDto>> GetPrivacyAsync(Guid userId, CancellationToken ct = default);
    Task<Result<FriendPrivacyDto>> UpdatePrivacyAsync(Guid userId, string policy, CancellationToken ct = default);
}
