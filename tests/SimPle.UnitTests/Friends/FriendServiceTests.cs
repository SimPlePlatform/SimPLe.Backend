using FluentAssertions;
using NSubstitute;
using SimPle.Application.Common.Interfaces;
using SimPle.Application.Friends.Services;
using SimPle.Domain.Friends;
using SimPle.Domain.Users;

namespace SimPle.UnitTests.Friends;

public sealed class FriendServiceTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IFriendRepository _friends = Substitute.For<IFriendRepository>();
    private readonly FriendService _service;

    private readonly User _alice = MakeUser("alice", "Alice Player");
    private readonly User _bob = MakeUser("bob", "Bob Player");

    public FriendServiceTests()
    {
        _service = new FriendService(_users, _friends);
        _users.GetByIdAsync(_alice.Id).Returns(_alice);
        _users.GetByIdAsync(_bob.Id).Returns(_bob);
        _friends.CountMutualFriendsAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(0);
        _friends.AreFriendsAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(false);
        _friends.IsBlockedEitherDirectionAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(false);
    }

    [Fact]
    public async Task SendRequest_success_creates_pending_request()
    {
        var result = await _service.SendRequestAsync(_alice.Id, _bob.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.User.UserId.Should().Be(_bob.Id);
        result.Value.Direction.Should().Be("Outgoing");
        await _friends.Received(1).AddRequestAsync(Arg.Is<FriendRequest>(r =>
            r.SenderUserId == _alice.Id &&
            r.ReceiverUserId == _bob.Id &&
            r.Status == FriendRequestStatus.Pending));
    }

    [Fact]
    public async Task GetFriends_and_requests_return_safe_dtos()
    {
        var request = FriendRequest.Create(_alice.Id, _bob.Id);
        _friends.GetFriendsAsync(_alice.Id).Returns(new[] { _bob });
        _friends.GetIncomingRequestsAsync(_alice.Id).Returns(new[] { request });
        _friends.GetOutgoingRequestsAsync(_alice.Id).Returns(new[] { request });

        var friendList = await _service.GetFriendsAsync(_alice.Id);
        var requests = await _service.GetRequestsAsync(_alice.Id);
        var incoming = await _service.GetIncomingRequestsAsync(_alice.Id);
        var outgoing = await _service.GetOutgoingRequestsAsync(_alice.Id);

        friendList.IsSuccess.Should().BeTrue();
        friendList.Value!.Single().Username.Should().Be("bob");
        requests.Value!.Incoming.Should().HaveCount(1);
        requests.Value.Outgoing.Should().HaveCount(1);
        incoming.Value!.Single().Status.Should().Be("Pending");
        outgoing.Value!.Single().Direction.Should().Be("Outgoing");
    }

    [Fact]
    public async Task SendRequest_rejects_self_request()
    {
        var result = await _service.SendRequestAsync(_alice.Id, _alice.Id);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("Friends.SelfRequest");
        await _friends.DidNotReceive().AddRequestAsync(Arg.Any<FriendRequest>());
    }

    [Fact]
    public async Task SendRequest_rejects_existing_friendship()
    {
        _friends.AreFriendsAsync(_alice.Id, _bob.Id).Returns(true);

        var result = await _service.SendRequestAsync(_alice.Id, _bob.Id);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("Friends.AlreadyFriends");
    }

    [Fact]
    public async Task SendRequest_rejects_duplicate_pending_request()
    {
        _friends.GetPendingRequestBetweenAsync(_alice.Id, _bob.Id).Returns(FriendRequest.Create(_alice.Id, _bob.Id));

        var result = await _service.SendRequestAsync(_alice.Id, _bob.Id);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("Friends.PendingRequestExists");
    }

    [Fact]
    public async Task SendRequest_rejects_when_blocked_either_direction()
    {
        _friends.IsBlockedEitherDirectionAsync(_alice.Id, _bob.Id).Returns(true);

        var result = await _service.SendRequestAsync(_alice.Id, _bob.Id);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("Friends.Blocked");
    }

    [Fact]
    public async Task SendRequest_privacy_off_rejects_request()
    {
        _bob.UpdateFriendRequestPolicy(FriendRequestPolicy.Off);

        var result = await _service.SendRequestAsync(_alice.Id, _bob.Id);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("Friends.Privacy");
    }

    [Fact]
    public async Task SendRequest_friends_of_friends_requires_mutual_friend()
    {
        _bob.UpdateFriendRequestPolicy(FriendRequestPolicy.FriendsOfFriends);
        _friends.CountMutualFriendsAsync(_alice.Id, _bob.Id).Returns(0);

        var denied = await _service.SendRequestAsync(_alice.Id, _bob.Id);
        denied.IsSuccess.Should().BeFalse();

        _friends.CountMutualFriendsAsync(_alice.Id, _bob.Id).Returns(1);
        var allowed = await _service.SendRequestAsync(_alice.Id, _bob.Id);
        allowed.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task AcceptRequest_only_receiver_can_accept_and_creates_friendship()
    {
        var request = FriendRequest.Create(_alice.Id, _bob.Id);
        _friends.GetRequestAsync(request.Id).Returns(request);

        var forbidden = await _service.AcceptRequestAsync(_alice.Id, request.Id);
        forbidden.IsSuccess.Should().BeFalse();
        forbidden.Error!.Code.Should().Be("Friends.Forbidden");

        var accepted = await _service.AcceptRequestAsync(_bob.Id, request.Id);
        accepted.IsSuccess.Should().BeTrue();
        request.Status.Should().Be(FriendRequestStatus.Accepted);
        await _friends.Received(1).AddFriendshipAsync(Arg.Is<Friendship>(f =>
            (f.UserId == _alice.Id && f.FriendUserId == _bob.Id) ||
            (f.UserId == _bob.Id && f.FriendUserId == _alice.Id)));
    }

    [Fact]
    public async Task DeclineRequest_only_receiver_can_decline()
    {
        var request = FriendRequest.Create(_alice.Id, _bob.Id);
        _friends.GetRequestAsync(request.Id).Returns(request);

        var result = await _service.DeclineRequestAsync(_bob.Id, request.Id);

        result.IsSuccess.Should().BeTrue();
        request.Status.Should().Be(FriendRequestStatus.Declined);
        await _friends.Received(1).UpdateRequestAsync(request);
    }

    [Fact]
    public async Task CancelRequest_only_sender_can_cancel()
    {
        var request = FriendRequest.Create(_alice.Id, _bob.Id);
        _friends.GetRequestAsync(request.Id).Returns(request);

        var result = await _service.CancelRequestAsync(_alice.Id, request.Id);

        result.IsSuccess.Should().BeTrue();
        request.Status.Should().Be(FriendRequestStatus.Cancelled);
        await _friends.Received(1).UpdateRequestAsync(request);
    }

    [Fact]
    public async Task RemoveFriend_requires_existing_friendship()
    {
        _friends.AreFriendsAsync(_alice.Id, _bob.Id).Returns(false);
        var missing = await _service.RemoveFriendAsync(_alice.Id, _bob.Id);
        missing.IsSuccess.Should().BeFalse();

        _friends.AreFriendsAsync(_alice.Id, _bob.Id).Returns(true);
        var removed = await _service.RemoveFriendAsync(_alice.Id, _bob.Id);
        removed.IsSuccess.Should().BeTrue();
        await _friends.Received(1).RemoveFriendshipAsync(_alice.Id, _bob.Id);
    }

    [Fact]
    public async Task Block_removes_relationship_state_and_creates_block()
    {
        var result = await _service.BlockAsync(_alice.Id, _bob.Id);

        result.IsSuccess.Should().BeTrue();
        await _friends.Received(1).ClearRelationshipAsync(_alice.Id, _bob.Id);
        await _friends.Received(1).AddBlockAsync(Arg.Is<UserBlock>(b =>
            b.BlockerUserId == _alice.Id && b.BlockedUserId == _bob.Id));
    }

    [Fact]
    public async Task Block_rejects_self_and_duplicate_block()
    {
        var self = await _service.BlockAsync(_alice.Id, _alice.Id);
        self.IsSuccess.Should().BeFalse();
        self.Error!.Code.Should().Be("Friends.SelfBlock");

        _friends.GetBlockAsync(_alice.Id, _bob.Id).Returns(UserBlock.Create(_alice.Id, _bob.Id));
        var duplicate = await _service.BlockAsync(_alice.Id, _bob.Id);
        duplicate.IsSuccess.Should().BeTrue();
        await _friends.DidNotReceive().AddBlockAsync(Arg.Is<UserBlock>(b =>
            b.BlockerUserId == _alice.Id && b.BlockedUserId == _bob.Id));
    }

    [Fact]
    public async Task Unblock_removes_user_block()
    {
        var result = await _service.UnblockAsync(_alice.Id, _bob.Id);

        result.IsSuccess.Should().BeTrue();
        await _friends.Received(1).RemoveBlockAsync(_alice.Id, _bob.Id);
    }

    [Fact]
    public async Task GetBlocks_returns_blocked_user_summaries()
    {
        _friends.GetBlocksAsync(_alice.Id).Returns(new[] { UserBlock.Create(_alice.Id, _bob.Id) });

        var result = await _service.GetBlocksAsync(_alice.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Single().Username.Should().Be("bob");
    }

    [Fact]
    public async Task Search_requires_two_characters_and_returns_safe_summaries()
    {
        var invalid = await _service.SearchAsync(_alice.Id, "a");
        invalid.IsSuccess.Should().BeFalse();

        _friends.SearchUsersAsync(_alice.Id, "bo", 20).Returns(new[] { _bob });
        var result = await _service.SearchAsync(_alice.Id, "bo");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Single().Username.Should().Be("bob");
        result.Value!.Single().DisplayName.Should().Be("Bob Player");
    }

    [Fact]
    public async Task Suggestions_returns_repository_suggestions_with_mutual_counts()
    {
        _friends.GetSuggestionsAsync(_alice.Id, 10).Returns(new[] { _bob });
        _friends.CountMutualFriendsAsync(_alice.Id, _bob.Id).Returns(2);

        var result = await _service.SuggestionsAsync(_alice.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Single().MutualFriendsCount.Should().Be(2);
    }

    [Fact]
    public async Task UpdatePrivacy_accepts_valid_policy_and_rejects_invalid()
    {
        var invalid = await _service.UpdatePrivacyAsync(_alice.Id, "Everywhere");
        invalid.IsSuccess.Should().BeFalse();

        var result = await _service.UpdatePrivacyAsync(_alice.Id, "FriendsOfFriends");
        result.IsSuccess.Should().BeTrue();
        result.Value!.FriendRequestPolicy.Should().Be("FriendsOfFriends");
        await _users.Received(1).UpdateAsync(_alice);
    }

    [Fact]
    public async Task GetPrivacy_returns_current_policy()
    {
        _alice.UpdateFriendRequestPolicy(FriendRequestPolicy.Off);

        var result = await _service.GetPrivacyAsync(_alice.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.FriendRequestPolicy.Should().Be("Off");
    }

    private static User MakeUser(string username, string displayName) =>
        User.Create(username, $"{username}@example.com", "hash", displayName);
}
