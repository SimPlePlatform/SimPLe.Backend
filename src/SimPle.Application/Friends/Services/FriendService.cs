using SimPle.Application.Common.Interfaces;
using SimPle.Application.Friends.DTOs;
using SimPle.Domain.Friends;
using SimPle.Domain.Users;
using SimPle.Shared.Common;

namespace SimPle.Application.Friends.Services;

public sealed class FriendService : IFriendService
{
    private const int SearchLimit = 20;
    private const int SuggestionLimit = 10;

    private readonly IUserRepository _users;
    private readonly IFriendRepository _friends;

    public FriendService(IUserRepository users, IFriendRepository friends)
    {
        _users = users;
        _friends = friends;
    }

    public async Task<Result<IReadOnlyList<UserSummaryDto>>> GetFriendsAsync(Guid userId, CancellationToken ct = default)
    {
        var users = await _friends.GetFriendsAsync(userId, ct);
        return Result<IReadOnlyList<UserSummaryDto>>.Ok(await ToSummariesAsync(userId, users, ct));
    }

    public async Task<Result<FriendRequestsDto>> GetRequestsAsync(Guid userId, CancellationToken ct = default)
    {
        var incoming = await GetIncomingRequestsAsync(userId, ct);
        var outgoing = await GetOutgoingRequestsAsync(userId, ct);
        if (!incoming.IsSuccess) return Result<FriendRequestsDto>.Fail(incoming.Error!);
        if (!outgoing.IsSuccess) return Result<FriendRequestsDto>.Fail(outgoing.Error!);
        return Result<FriendRequestsDto>.Ok(new(incoming.Value!, outgoing.Value!));
    }

    public async Task<Result<IReadOnlyList<FriendRequestDto>>> GetIncomingRequestsAsync(Guid userId, CancellationToken ct = default)
    {
        var requests = await _friends.GetIncomingRequestsAsync(userId, ct);
        return Result<IReadOnlyList<FriendRequestDto>>.Ok(await ToRequestDtosAsync(userId, requests, incoming: true, ct));
    }

    public async Task<Result<IReadOnlyList<FriendRequestDto>>> GetOutgoingRequestsAsync(Guid userId, CancellationToken ct = default)
    {
        var requests = await _friends.GetOutgoingRequestsAsync(userId, ct);
        return Result<IReadOnlyList<FriendRequestDto>>.Ok(await ToRequestDtosAsync(userId, requests, incoming: false, ct));
    }

    public async Task<Result<FriendRequestDto>> SendRequestAsync(Guid senderUserId, Guid receiverUserId, CancellationToken ct = default)
    {
        if (senderUserId == receiverUserId)
            return Result<FriendRequestDto>.Fail("Friends.SelfRequest", "You cannot send a friend request to yourself.");

        var sender = await _users.GetByIdAsync(senderUserId, ct);
        var receiver = await _users.GetByIdAsync(receiverUserId, ct);
        if (sender is null || receiver is null)
            return Result<FriendRequestDto>.Fail("General.NotFound", "User not found.");

        if (await _friends.IsBlockedEitherDirectionAsync(senderUserId, receiverUserId, ct))
            return Result<FriendRequestDto>.Fail("Friends.Blocked", "Friend requests are not allowed between these users.");

        if (await _friends.AreFriendsAsync(senderUserId, receiverUserId, ct))
            return Result<FriendRequestDto>.Fail("Friends.AlreadyFriends", "You are already friends.");

        if (await _friends.GetPendingRequestBetweenAsync(senderUserId, receiverUserId, ct) is not null)
            return Result<FriendRequestDto>.Fail("Friends.PendingRequestExists", "A pending friend request already exists.");

        if (!await CanSendRequestByPrivacyAsync(senderUserId, receiver, ct))
            return Result<FriendRequestDto>.Fail("Friends.Privacy", "This user is not accepting friend requests from you.");

        var request = FriendRequest.Create(senderUserId, receiverUserId);
        await _friends.AddRequestAsync(request, ct);
        return Result<FriendRequestDto>.Ok(await ToRequestDtoAsync(senderUserId, request, incoming: false, ct));
    }

    public async Task<Result<FriendRequestDto>> AcceptRequestAsync(Guid userId, Guid requestId, CancellationToken ct = default)
    {
        var request = await _friends.GetRequestAsync(requestId, ct);
        if (request is null) return Result<FriendRequestDto>.Fail("Friends.RequestNotFound", "Friend request not found.");
        if (request.ReceiverUserId != userId) return Result<FriendRequestDto>.Fail("Friends.Forbidden", "Only the receiver can accept this request.");
        if (request.Status != FriendRequestStatus.Pending) return Result<FriendRequestDto>.Fail("Friends.RequestClosed", "This request is no longer pending.");
        if (await _friends.IsBlockedEitherDirectionAsync(request.SenderUserId, request.ReceiverUserId, ct))
            return Result<FriendRequestDto>.Fail("Friends.Blocked", "Blocked users cannot become friends.");

        request.Accept();
        if (!await _friends.AreFriendsAsync(request.SenderUserId, request.ReceiverUserId, ct))
            await _friends.AddFriendshipAsync(Friendship.Create(request.SenderUserId, request.ReceiverUserId), ct);
        await _friends.UpdateRequestAsync(request, ct);

        return Result<FriendRequestDto>.Ok(await ToRequestDtoAsync(userId, request, incoming: true, ct));
    }

    public async Task<Result<FriendRequestDto>> DeclineRequestAsync(Guid userId, Guid requestId, CancellationToken ct = default)
    {
        var request = await _friends.GetRequestAsync(requestId, ct);
        if (request is null) return Result<FriendRequestDto>.Fail("Friends.RequestNotFound", "Friend request not found.");
        if (request.ReceiverUserId != userId) return Result<FriendRequestDto>.Fail("Friends.Forbidden", "Only the receiver can decline this request.");
        if (request.Status != FriendRequestStatus.Pending) return Result<FriendRequestDto>.Fail("Friends.RequestClosed", "This request is no longer pending.");

        request.Decline();
        await _friends.UpdateRequestAsync(request, ct);
        return Result<FriendRequestDto>.Ok(await ToRequestDtoAsync(userId, request, incoming: true, ct));
    }

    public async Task<Result<FriendRequestDto>> CancelRequestAsync(Guid userId, Guid requestId, CancellationToken ct = default)
    {
        var request = await _friends.GetRequestAsync(requestId, ct);
        if (request is null) return Result<FriendRequestDto>.Fail("Friends.RequestNotFound", "Friend request not found.");
        if (request.SenderUserId != userId) return Result<FriendRequestDto>.Fail("Friends.Forbidden", "Only the sender can cancel this request.");
        if (request.Status != FriendRequestStatus.Pending) return Result<FriendRequestDto>.Fail("Friends.RequestClosed", "This request is no longer pending.");

        request.Cancel();
        await _friends.UpdateRequestAsync(request, ct);
        return Result<FriendRequestDto>.Ok(await ToRequestDtoAsync(userId, request, incoming: false, ct));
    }

    public async Task<Result> RemoveFriendAsync(Guid userId, Guid friendUserId, CancellationToken ct = default)
    {
        if (!await _friends.AreFriendsAsync(userId, friendUserId, ct))
            return Result.Fail("Friends.NotFriends", "Friendship not found.");

        await _friends.RemoveFriendshipAsync(userId, friendUserId, ct);
        return Result.Ok();
    }

    public async Task<Result<IReadOnlyList<UserSummaryDto>>> SearchAsync(Guid userId, string query, CancellationToken ct = default)
    {
        if (query.Trim().Length < 2)
            return Result<IReadOnlyList<UserSummaryDto>>.Fail("Validation.Failed", "Search query must be at least 2 characters.");

        var users = await _friends.SearchUsersAsync(userId, query.Trim(), SearchLimit, ct);
        return Result<IReadOnlyList<UserSummaryDto>>.Ok(await ToSummariesAsync(userId, users, ct));
    }

    public async Task<Result<IReadOnlyList<UserSummaryDto>>> SuggestionsAsync(Guid userId, CancellationToken ct = default)
    {
        var users = await _friends.GetSuggestionsAsync(userId, SuggestionLimit, ct);
        return Result<IReadOnlyList<UserSummaryDto>>.Ok(await ToSummariesAsync(userId, users, ct));
    }

    public async Task<Result<IReadOnlyList<BlockedUserDto>>> GetBlocksAsync(Guid userId, CancellationToken ct = default)
    {
        var blocks = await _friends.GetBlocksAsync(userId, ct);
        var dtos = new List<BlockedUserDto>();
        foreach (var block in blocks)
        {
            var user = await _users.GetByIdAsync(block.BlockedUserId, ct);
            if (user is null) continue;
            dtos.Add(new(user.Id, user.Username, user.DisplayName, user.AvatarUrl, user.Color, user.Initials, block.CreatedAt));
        }
        return Result<IReadOnlyList<BlockedUserDto>>.Ok(dtos);
    }

    public async Task<Result<BlockedUserDto>> BlockAsync(Guid blockerUserId, Guid blockedUserId, CancellationToken ct = default)
    {
        if (blockerUserId == blockedUserId)
            return Result<BlockedUserDto>.Fail("Friends.SelfBlock", "You cannot block yourself.");

        var blocked = await _users.GetByIdAsync(blockedUserId, ct);
        if (blocked is null) return Result<BlockedUserDto>.Fail("General.NotFound", "User not found.");

        await _friends.ClearRelationshipAsync(blockerUserId, blockedUserId, ct);
        var existing = await _friends.GetBlockAsync(blockerUserId, blockedUserId, ct);
        var block = existing ?? UserBlock.Create(blockerUserId, blockedUserId);
        if (existing is null) await _friends.AddBlockAsync(block, ct);

        return Result<BlockedUserDto>.Ok(new(blocked.Id, blocked.Username, blocked.DisplayName, blocked.AvatarUrl, blocked.Color, blocked.Initials, block.CreatedAt));
    }

    public async Task<Result> UnblockAsync(Guid blockerUserId, Guid blockedUserId, CancellationToken ct = default)
    {
        await _friends.RemoveBlockAsync(blockerUserId, blockedUserId, ct);
        return Result.Ok();
    }

    public async Task<Result<FriendPrivacyDto>> GetPrivacyAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return Result<FriendPrivacyDto>.Fail("General.NotFound", "User not found.");
        return Result<FriendPrivacyDto>.Ok(new(user.FriendRequestPolicy.ToString()));
    }

    public async Task<Result<FriendPrivacyDto>> UpdatePrivacyAsync(Guid userId, string policy, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return Result<FriendPrivacyDto>.Fail("General.NotFound", "User not found.");
        if (!Enum.TryParse<FriendRequestPolicy>(policy, ignoreCase: true, out var parsed))
            return Result<FriendPrivacyDto>.Fail("Validation.Failed", "Invalid friend request privacy setting.");

        user.UpdateFriendRequestPolicy(parsed);
        await _users.UpdateAsync(user, ct);
        return Result<FriendPrivacyDto>.Ok(new(user.FriendRequestPolicy.ToString()));
    }

    private async Task<bool> CanSendRequestByPrivacyAsync(Guid senderUserId, User receiver, CancellationToken ct)
    {
        return receiver.FriendRequestPolicy switch
        {
            FriendRequestPolicy.Anyone => true,
            FriendRequestPolicy.Off => false,
            FriendRequestPolicy.FriendsOfFriends => await _friends.CountMutualFriendsAsync(senderUserId, receiver.Id, ct) > 0,
            _ => false
        };
    }

    private async Task<IReadOnlyList<UserSummaryDto>> ToSummariesAsync(Guid requesterId, IReadOnlyList<User> users, CancellationToken ct)
    {
        var result = new List<UserSummaryDto>();
        foreach (var user in users)
            result.Add(await ToSummaryAsync(requesterId, user, ct));
        return result;
    }

    private async Task<IReadOnlyList<FriendRequestDto>> ToRequestDtosAsync(
        Guid viewerId, IReadOnlyList<FriendRequest> requests, bool incoming, CancellationToken ct)
    {
        var result = new List<FriendRequestDto>();
        foreach (var request in requests)
            result.Add(await ToRequestDtoAsync(viewerId, request, incoming, ct));
        return result;
    }

    private async Task<FriendRequestDto> ToRequestDtoAsync(Guid viewerId, FriendRequest request, bool incoming, CancellationToken ct)
    {
        var otherUserId = incoming ? request.SenderUserId : request.ReceiverUserId;
        var otherUser = await _users.GetByIdAsync(otherUserId, ct)
            ?? throw new InvalidOperationException("Friend request references a missing user.");
        return new FriendRequestDto(
            request.Id,
            await ToSummaryAsync(viewerId, otherUser, ct),
            incoming ? "Incoming" : "Outgoing",
            request.Status.ToString(),
            request.CreatedAt,
            request.RespondedAtUtc,
            request.CancelledAtUtc);
    }

    private async Task<UserSummaryDto> ToSummaryAsync(Guid requesterId, User user, CancellationToken ct)
    {
        return new UserSummaryDto(
            user.Id,
            user.Username,
            user.DisplayName,
            user.AvatarUrl,
            user.Color,
            user.Initials,
            user.ProfileType.ToString(),
            await _friends.CountMutualFriendsAsync(requesterId, user.Id, ct),
            await GetFriendshipStatusAsync(requesterId, user.Id, ct));
    }

    private async Task<string> GetFriendshipStatusAsync(Guid viewerId, Guid otherUserId, CancellationToken ct)
    {
        if (viewerId == otherUserId) return "Self";
        if (await _friends.IsBlockedEitherDirectionAsync(viewerId, otherUserId, ct)) return "Blocked";
        if (await _friends.AreFriendsAsync(viewerId, otherUserId, ct)) return "Friends";
        var pending = await _friends.GetPendingRequestBetweenAsync(viewerId, otherUserId, ct);
        if (pending is null) return "None";
        return pending.SenderUserId == viewerId ? "RequestSent" : "RequestReceived";
    }
}
