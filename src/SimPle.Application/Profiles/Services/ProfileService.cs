using SimPle.Application.Common.Interfaces;
using SimPle.Application.Profiles.DTOs;
using SimPle.Domain.Profiles;
using SimPle.Domain.Users;
using SimPle.Shared.Common;

namespace SimPle.Application.Profiles.Services;

public sealed class ProfileService : IProfileService
{
    private readonly IUserRepository _users;
    private readonly IProfileRepository _profiles;

    public ProfileService(IUserRepository users, IProfileRepository profiles)
    {
        _users = users;
        _profiles = profiles;
    }

    public async Task<Result<ProfileDto>> GetMyProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return Result<ProfileDto>.Fail("General.NotFound", "User not found.");

        var links = await _profiles.GetLinksByUserIdAsync(userId, ct);
        var interests = await _profiles.GetInterestsByUserIdAsync(userId, ct);
        return Result<ProfileDto>.Ok(ToDto(user, links, interests));
    }

    public async Task<Result<ProfileDto>> GetPublicProfileAsync(
        string username, Guid? requesterId, CancellationToken ct = default)
    {
        var normalized = username.Trim().ToUpperInvariant();
        var user = await _users.GetByNormalizedUsernameAsync(normalized, ct);
        if (user is null) return Result<ProfileDto>.Fail("General.NotFound", "Profile not found.");

        // Private: visible only to owner.
        if (user.Visibility == ProfileVisibility.Private && user.Id != requesterId)
            return Result<ProfileDto>.Fail("Profile.Private", "This profile is private.");

        // FriendsOnly: treated as owner-only until Module 3 friends are implemented.
        if (user.Visibility == ProfileVisibility.FriendsOnly && user.Id != requesterId)
            return Result<ProfileDto>.Fail("Profile.FriendsOnly", "This profile is visible to friends only.");

        var links = await _profiles.GetLinksByUserIdAsync(user.Id, ct);
        var interests = await _profiles.GetInterestsByUserIdAsync(user.Id, ct);
        return Result<ProfileDto>.Ok(ToDto(user, links, interests));
    }

    public async Task<Result<ProfileDto>> UpdateProfileAsync(
        Guid userId, UpdateProfileRequestDto request, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return Result<ProfileDto>.Fail("General.NotFound", "User not found.");

        ProfileVisibility? visibility = null;
        if (request.Visibility is not null &&
            Enum.TryParse<ProfileVisibility>(request.Visibility, out var parsed))
            visibility = parsed;

        user.UpdateProfile(
            request.DisplayName,
            request.Bio,
            request.AvatarUrl,
            request.BannerUrl,
            request.Region,
            request.StatusMessage,
            visibility);

        await _users.UpdateAsync(user, ct);

        var links = await _profiles.GetLinksByUserIdAsync(userId, ct);
        var interests = await _profiles.GetInterestsByUserIdAsync(userId, ct);
        return Result<ProfileDto>.Ok(ToDto(user, links, interests));
    }

    public async Task<Result> UpdateUsernameAsync(Guid userId, string newUsername, CancellationToken ct = default)
    {
        var normalized = newUsername.Trim().ToUpperInvariant();
        if (await _users.ExistsByUsernameAsync(normalized, ct))
            return Result.Fail("Profile.UsernameTaken", "That username is already in use.");

        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return Result.Fail("General.NotFound", "User not found.");

        user.UpdateUsername(newUsername);
        await _users.UpdateAsync(user, ct);
        return Result.Ok();
    }

    public async Task<Result<IReadOnlyList<ExternalLinkDto>>> GetLinksAsync(
        Guid userId, CancellationToken ct = default)
    {
        var links = await _profiles.GetLinksByUserIdAsync(userId, ct);
        return Result<IReadOnlyList<ExternalLinkDto>>.Ok(links.Select(ToLinkDto).ToList());
    }

    public async Task<Result<IReadOnlyList<ExternalLinkDto>>> UpdateLinksAsync(
        Guid userId, UpdateLinksRequestDto request, CancellationToken ct = default)
    {
        var newLinks = request.Links
            .Select((l, i) => ProfileExternalLink.Create(userId, l.Platform, l.Url, l.DisplayLabel, i))
            .ToList();

        await _profiles.ReplaceLinksAsync(userId, newLinks, ct);
        return Result<IReadOnlyList<ExternalLinkDto>>.Ok(newLinks.Select(ToLinkDto).ToList());
    }

    public async Task<Result<IReadOnlyList<string>>> GetInterestsAsync(
        Guid userId, CancellationToken ct = default)
    {
        var tags = await _profiles.GetInterestsByUserIdAsync(userId, ct);
        return Result<IReadOnlyList<string>>.Ok(tags.Select(t => t.Name).ToList());
    }

    public async Task<Result<IReadOnlyList<string>>> UpdateInterestsAsync(
        Guid userId, UpdateInterestsRequestDto request, CancellationToken ct = default)
    {
        var tags = request.Interests
            .Select(name => ProfileInterestTag.Create(userId, name))
            .ToList();

        await _profiles.ReplaceInterestsAsync(userId, tags, ct);
        return Result<IReadOnlyList<string>>.Ok(tags.Select(t => t.Name).ToList());
    }

    private static ProfileDto ToDto(
        User user,
        IReadOnlyList<ProfileExternalLink> links,
        IReadOnlyList<ProfileInterestTag> interests) => new(
            UserId: user.Id,
            Username: user.Username,
            DisplayName: user.DisplayName,
            Bio: user.Bio,
            AvatarUrl: user.AvatarUrl,
            BannerUrl: user.BannerUrl,
            StatusMessage: user.StatusMessage,
            Region: user.Region,
            Color: user.Color,
            Initials: user.Initials,
            Visibility: user.Visibility.ToString(),
            Role: user.Role.ToString(),
            Level: user.Level,
            Elo: user.Elo,
            JoinedAt: user.CreatedAt,
            Links: links.Select(ToLinkDto).ToList(),
            Interests: interests.Select(t => t.Name).ToList());

    private static ExternalLinkDto ToLinkDto(ProfileExternalLink l) => new(
        l.Id, l.Platform, l.Url, l.DisplayLabel, l.SortOrder);
}
