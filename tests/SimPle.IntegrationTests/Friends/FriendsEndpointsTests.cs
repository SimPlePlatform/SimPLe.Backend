using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SimPle.Application.Friends.DTOs;
using SimPle.IntegrationTests.Auth;

namespace SimPle.IntegrationTests.Friends;

public sealed class FriendsEndpointsTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory = new();

    [Fact]
    public async Task Friend_endpoints_require_authentication()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/api/friends");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Request_accept_friend_list_flow_returns_safe_dtos()
    {
        using var alice = CreateClient();
        using var bob = CreateClient();
        await RegisterAndLoginAsync(alice, "alice", "alice@example.com");
        await RegisterAndLoginAsync(bob, "bob", "bob@example.com");
        var bobId = await SearchUserIdAsync(alice, "bob");

        var requestResponse = await alice.PostAsJsonAsync("/api/friends/requests", new SendFriendRequestDto(bobId));
        requestResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var incoming = await bob.GetFromJsonAsync<IReadOnlyList<FriendRequestDto>>("/api/friends/requests/incoming");
        incoming.Should().NotBeNull();
        var requestId = incoming!.Single().Id;

        var accept = await bob.PostAsync($"/api/friends/requests/{requestId}/accept", null);
        accept.StatusCode.Should().Be(HttpStatusCode.OK);

        var aliceFriends = await alice.GetFromJsonAsync<IReadOnlyList<UserSummaryDto>>("/api/friends");
        aliceFriends.Should().NotBeNull();
        aliceFriends!.Single().Username.Should().Be("bob");
        aliceFriends.Single().GetType().GetProperties().Select(p => p.Name)
            .Should().NotContain(new[] { "Email", "PasswordHash", "RefreshTokens" });
    }

    [Fact]
    public async Task Request_decline_and_cancel_flows_work()
    {
        using var alice = CreateClient();
        using var bob = CreateClient();
        await RegisterAndLoginAsync(alice, "alice", "alice@example.com");
        await RegisterAndLoginAsync(bob, "bob", "bob@example.com");
        var bobId = await SearchUserIdAsync(alice, "bob");

        var first = await alice.PostAsJsonAsync("/api/friends/requests", new SendFriendRequestDto(bobId));
        var firstDto = await first.Content.ReadFromJsonAsync<FriendRequestDto>();
        var decline = await bob.PostAsync($"/api/friends/requests/{firstDto!.Id}/decline", null);
        decline.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await alice.PostAsJsonAsync("/api/friends/requests", new SendFriendRequestDto(bobId));
        var secondDto = await second.Content.ReadFromJsonAsync<FriendRequestDto>();
        var cancel = await alice.PostAsync($"/api/friends/requests/{secondDto!.Id}/cancel", null);
        cancel.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Block_prevents_friend_request()
    {
        using var alice = CreateClient();
        using var bob = CreateClient();
        await RegisterAndLoginAsync(alice, "alice", "alice@example.com");
        await RegisterAndLoginAsync(bob, "bob", "bob@example.com");
        var aliceId = await SearchUserIdAsync(bob, "alice");
        var bobId = await SearchUserIdAsync(alice, "bob");

        var block = await alice.PostAsJsonAsync("/api/blocks", new BlockUserRequestDto(bobId));
        block.StatusCode.Should().Be(HttpStatusCode.OK);

        var request = await bob.PostAsJsonAsync("/api/friends/requests", new SendFriendRequestDto(aliceId));
        request.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await request.Content.ReadAsStringAsync()).Should().Contain("Friends.Blocked");
    }

    [Fact]
    public async Task Privacy_off_rejects_friend_requests()
    {
        using var alice = CreateClient();
        using var bob = CreateClient();
        await RegisterAndLoginAsync(alice, "alice", "alice@example.com");
        await RegisterAndLoginAsync(bob, "bob", "bob@example.com");
        var bobId = await SearchUserIdAsync(alice, "bob");

        var privacy = await bob.PutAsJsonAsync("/api/friends/privacy", new UpdateFriendPrivacyDto("Off"));
        privacy.StatusCode.Should().Be(HttpStatusCode.OK);

        var request = await alice.PostAsJsonAsync("/api/friends/requests", new SendFriendRequestDto(bobId));
        request.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await request.Content.ReadAsStringAsync()).Should().Contain("Friends.Privacy");
    }

    [Fact]
    public async Task Search_suggestions_privacy_block_and_remove_endpoints_work()
    {
        using var alice = CreateClient();
        using var bob = CreateClient();
        using var cara = CreateClient();
        await RegisterAndLoginAsync(alice, "alice", "alice@example.com");
        await RegisterAndLoginAsync(bob, "bob", "bob@example.com");
        await RegisterAndLoginAsync(cara, "cara", "cara@example.com");
        var bobId = await SearchUserIdAsync(alice, "bob");
        var caraId = await SearchUserIdAsync(alice, "cara");

        var search = await alice.GetFromJsonAsync<IReadOnlyList<UserSummaryDto>>("/api/friends/search?query=bo");
        search.Should().NotBeNull();
        search!.Single().Username.Should().Be("bob");

        var privacy = await alice.PutAsJsonAsync("/api/friends/privacy", new UpdateFriendPrivacyDto("FriendsOfFriends"));
        privacy.StatusCode.Should().Be(HttpStatusCode.OK);
        var privacyDto = await alice.GetFromJsonAsync<FriendPrivacyDto>("/api/friends/privacy");
        privacyDto!.FriendRequestPolicy.Should().Be("FriendsOfFriends");

        await alice.PostAsJsonAsync("/api/friends/requests", new SendFriendRequestDto(bobId));
        var incoming = await bob.GetFromJsonAsync<IReadOnlyList<FriendRequestDto>>("/api/friends/requests/incoming");
        await bob.PostAsync($"/api/friends/requests/{incoming!.Single().Id}/accept", null);

        var suggestions = await alice.GetFromJsonAsync<IReadOnlyList<UserSummaryDto>>("/api/friends/suggestions");
        suggestions.Should().NotBeNull();

        var block = await alice.PostAsJsonAsync("/api/blocks", new BlockUserRequestDto(caraId));
        block.StatusCode.Should().Be(HttpStatusCode.OK);
        var blocks = await alice.GetFromJsonAsync<IReadOnlyList<BlockedUserDto>>("/api/blocks");
        blocks!.Single().Username.Should().Be("cara");
        var unblock = await alice.DeleteAsync($"/api/blocks/{caraId}");
        unblock.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var remove = await alice.DeleteAsync($"/api/friends/{bobId}");
        remove.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var friends = await alice.GetFromJsonAsync<IReadOnlyList<UserSummaryDto>>("/api/friends");
        friends.Should().BeEmpty();
    }

    private async Task<Guid> SearchUserIdAsync(HttpClient client, string query)
    {
        var results = await client.GetFromJsonAsync<IReadOnlyList<UserSummaryDto>>($"/api/friends/search?query={query}");
        results.Should().NotBeNull();
        return results!.Single(u => u.Username == query).UserId;
    }

    private async Task RegisterAndLoginAsync(HttpClient client, string username, string email)
    {
        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            Username = username,
            Email = email,
            Password = "valid password phrase",
            ConfirmPassword = "valid password phrase",
            CaptchaToken = "test-captcha-token"
        });
        register.StatusCode.Should().Be(HttpStatusCode.Created);

        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            EmailOrUsername = email,
            Password = "valid password phrase",
            CaptchaToken = "test-captcha-token"
        });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private HttpClient CreateClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true
        });
        client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        return client;
    }

    public void Dispose() => _factory.Dispose();
}
