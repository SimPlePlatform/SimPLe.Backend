using FluentAssertions;
using NSubstitute;
using SimPle.Application.Common.Interfaces;
using SimPle.Application.Profiles.DTOs;
using SimPle.Application.Profiles.Services;
using SimPle.Application.Profiles.Validators;
using SimPle.Domain.Profiles;
using SimPle.Domain.Users;

namespace SimPle.UnitTests.Profiles;

public sealed class ProfileServiceTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IProfileRepository _profiles = Substitute.For<IProfileRepository>();
    private readonly ProfileService _service;

    public ProfileServiceTests()
    {
        _profiles.GetLinksByUserIdAsync(Arg.Any<Guid>()).Returns((IReadOnlyList<ProfileExternalLink>)[]);
        _profiles.GetInterestsByUserIdAsync(Arg.Any<Guid>()).Returns((IReadOnlyList<ProfileInterestTag>)[]);
        _service = new ProfileService(_users, _profiles);
    }

    private static User MakeUser(string username = "testuser") =>
        User.Create(username, $"{username}@example.com", "hash", "Test User");

    // ── GetMyProfile ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyProfile_ExistingUser_ReturnsProfileDto()
    {
        var user = MakeUser();
        _users.GetByIdAsync(user.Id).Returns(user);

        var result = await _service.GetMyProfileAsync(user.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.UserId.Should().Be(user.Id);
        result.Value.Username.Should().Be("testuser");
        result.Value.Visibility.Should().Be("Public");
    }

    [Fact]
    public async Task GetMyProfile_UserNotFound_Fails()
    {
        _users.GetByIdAsync(Arg.Any<Guid>()).Returns((User?)null);

        var result = await _service.GetMyProfileAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("General.NotFound");
    }

    // ── GetPublicProfile ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetPublicProfile_PublicUser_VisibleToAnyone()
    {
        var user = MakeUser();
        _users.GetByNormalizedUsernameAsync("TESTUSER").Returns(user);

        var result = await _service.GetPublicProfileAsync("testuser", null);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Username.Should().Be("testuser");
    }

    [Fact]
    public async Task GetPublicProfile_PrivateUser_HiddenFromOthers()
    {
        var user = MakeUser();
        user.UpdateProfile("Test", null, null, null, visibility: ProfileVisibility.Private);
        _users.GetByNormalizedUsernameAsync("TESTUSER").Returns(user);

        var result = await _service.GetPublicProfileAsync("testuser", Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("Profile.Private");
    }

    [Fact]
    public async Task GetPublicProfile_PrivateUser_VisibleToOwner()
    {
        var user = MakeUser();
        user.UpdateProfile("Test", null, null, null, visibility: ProfileVisibility.Private);
        _users.GetByNormalizedUsernameAsync("TESTUSER").Returns(user);

        var result = await _service.GetPublicProfileAsync("testuser", user.Id);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetPublicProfile_FriendsOnly_TreatedAsOwnerOnly()
    {
        var user = MakeUser();
        user.UpdateProfile("Test", null, null, null, visibility: ProfileVisibility.FriendsOnly);
        _users.GetByNormalizedUsernameAsync("TESTUSER").Returns(user);

        var result = await _service.GetPublicProfileAsync("testuser", Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("Profile.FriendsOnly");
    }

    [Fact]
    public async Task GetPublicProfile_NotFound_Fails()
    {
        _users.GetByNormalizedUsernameAsync(Arg.Any<string>()).Returns((User?)null);

        var result = await _service.GetPublicProfileAsync("ghost", null);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("General.NotFound");
    }

    // ── UpdateProfile ─────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateProfile_ValidRequest_UpdatesAndReturnsDto()
    {
        var user = MakeUser();
        _users.GetByIdAsync(user.Id).Returns(user);

        var result = await _service.UpdateProfileAsync(user.Id, new UpdateProfileRequestDto(
            DisplayName: "Updated Name",
            Bio: "My bio",
            AvatarUrl: null,
            BannerUrl: null,
            Region: "NA-East",
            StatusMessage: "Playing!",
            Visibility: "FriendsOnly"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.DisplayName.Should().Be("Updated Name");
        result.Value.Bio.Should().Be("My bio");
        result.Value.StatusMessage.Should().Be("Playing!");
        result.Value.Visibility.Should().Be("FriendsOnly");
        await _users.Received(1).UpdateAsync(Arg.Any<User>());
    }

    [Fact]
    public async Task UpdateProfile_InvalidVisibility_StillSucceeds_WithNoChange()
    {
        var user = MakeUser();
        _users.GetByIdAsync(user.Id).Returns(user);

        // If the visibility string doesn't parse, the enum stays at its current value.
        var result = await _service.UpdateProfileAsync(user.Id, new UpdateProfileRequestDto(
            "Name", null, null, null, null, null, "InvalidEnum"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Visibility.Should().Be("Public"); // unchanged default
    }

    // ── UpdateUsername ────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateUsername_Available_Succeeds()
    {
        var user = MakeUser();
        _users.ExistsByUsernameAsync("NEWHANDLE").Returns(false);
        _users.GetByIdAsync(user.Id).Returns(user);

        var result = await _service.UpdateUsernameAsync(user.Id, "newhandle");

        result.IsSuccess.Should().BeTrue();
        await _users.Received(1).UpdateAsync(Arg.Is<User>(u => u.Username == "newhandle"));
    }

    [Fact]
    public async Task UpdateUsername_Taken_Fails()
    {
        _users.ExistsByUsernameAsync(Arg.Any<string>()).Returns(true);

        var result = await _service.UpdateUsernameAsync(Guid.NewGuid(), "takenname");

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("Profile.UsernameTaken");
    }

    // ── UpdateLinks ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateLinks_ValidLinks_PersistsAndReturns()
    {
        var userId = Guid.NewGuid();
        var request = new UpdateLinksRequestDto(
        [
            new LinkItemDto("github", "https://github.com/test", null, 0),
            new LinkItemDto("twitter", "https://twitter.com/test", "@test", 1),
        ]);

        var result = await _service.UpdateLinksAsync(userId, request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCount(2);
        result.Value[0].Platform.Should().Be("github");
        await _profiles.Received(1).ReplaceLinksAsync(userId, Arg.Is<IReadOnlyList<ProfileExternalLink>>(l => l.Count == 2));
    }

    [Fact]
    public async Task UpdateLinks_EmptyList_ClearsLinks()
    {
        var userId = Guid.NewGuid();
        var result = await _service.UpdateLinksAsync(userId, new UpdateLinksRequestDto([]));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().BeEmpty();
        await _profiles.Received(1).ReplaceLinksAsync(userId, Arg.Is<IReadOnlyList<ProfileExternalLink>>(l => l.Count == 0));
    }

    // ── UpdateInterests ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateInterests_ValidTags_PersistsAndReturns()
    {
        var userId = Guid.NewGuid();
        var request = new UpdateInterestsRequestDto(["board-games", "puzzle-games"]);

        var result = await _service.UpdateInterestsAsync(userId, request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().BeEquivalentTo(["board-games", "puzzle-games"]);
        await _profiles.Received(1).ReplaceInterestsAsync(
            userId, Arg.Is<IReadOnlyList<ProfileInterestTag>>(t => t.Count == 2));
    }

    // ── Validators ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("", null, null, null)]        // empty display name
    [InlineData("Name", null, "not-https", null)]  // avatar not https
    public async Task UpdateProfileValidator_InvalidInput_HasErrors(
        string displayName, string? bio, string? avatarUrl, string? bannerUrl)
    {
        var validator = new UpdateProfileRequestValidator();
        var dto = new UpdateProfileRequestDto(displayName, bio, avatarUrl, bannerUrl, null, null, null);
        var result = await validator.ValidateAsync(dto, CancellationToken.None);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateProfileValidator_ValidInput_Passes()
    {
        var validator = new UpdateProfileRequestValidator();
        var dto = new UpdateProfileRequestDto(
            "Test User", "Bio text", "https://example.com/avatar.png",
            null, "EU-West", null, "Public");
        var result = await validator.ValidateAsync(dto, CancellationToken.None);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("ab")]          // too short
    [InlineData("has space")]   // invalid chars
    [InlineData("a@user")]      // invalid chars
    public async Task UsernameValidator_InvalidUsername_HasErrors(string username)
    {
        var validator = new UpdateUsernameRequestValidator();
        var dto = new UpdateUsernameRequestDto(username);
        var result = await validator.ValidateAsync(dto, CancellationToken.None);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("validuser")]
    [InlineData("user_123")]
    [InlineData("some.handle-1")]
    public async Task UsernameValidator_ValidUsername_Passes(string username)
    {
        var validator = new UpdateUsernameRequestValidator();
        var dto = new UpdateUsernameRequestDto(username);
        var result = await validator.ValidateAsync(dto, CancellationToken.None);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task LinksValidator_InvalidPlatform_HasErrors()
    {
        var validator = new UpdateLinksRequestValidator();
        var dto = new UpdateLinksRequestDto([new LinkItemDto("fakebook", "https://fb.com", null, 0)]);
        var result = await validator.ValidateAsync(dto, CancellationToken.None);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task InterestsValidator_InvalidInterest_HasErrors()
    {
        var validator = new UpdateInterestsRequestValidator();
        var dto = new UpdateInterestsRequestDto(["not-a-real-interest"]);
        var result = await validator.ValidateAsync(dto, CancellationToken.None);
        result.IsValid.Should().BeFalse();
    }
}
