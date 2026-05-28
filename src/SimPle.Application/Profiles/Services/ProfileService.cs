using Microsoft.Extensions.Options;
using SimPle.Application.Common.Interfaces;
using SimPle.Application.Common.Options;
using SimPle.Application.Profiles.DTOs;
using SimPle.Domain.Profiles;
using SimPle.Domain.Users;
using SimPle.Shared.Common;

namespace SimPle.Application.Profiles.Services;

public sealed class ProfileService : IProfileService
{
    private readonly IUserRepository _users;
    private readonly IProfileRepository _profiles;
    private readonly IUsernameChangeRequestRepository _usernameRequests;
    private readonly IFileStorageService _storage;
    private readonly AwsOptions _aws;

    private static readonly HashSet<string> AllowedImageTypes =
        new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp" };

    public ProfileService(
        IUserRepository users,
        IProfileRepository profiles,
        IUsernameChangeRequestRepository usernameRequests,
        IFileStorageService storage,
        IOptions<AwsOptions> awsOptions)
    {
        _users = users;
        _profiles = profiles;
        _usernameRequests = usernameRequests;
        _storage = storage;
        _aws = awsOptions.Value;
    }

    public async Task<Result<ProfileDto>> GetMyProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return Result<ProfileDto>.Fail("General.NotFound", "User not found.");

        return await BuildDtoAsync(user, ct);
    }

    public async Task<Result<ProfileDto>> GetPublicProfileAsync(
        string username, Guid? requesterId, CancellationToken ct = default)
    {
        var normalized = username.Trim().ToUpperInvariant();
        var user = await _users.GetByNormalizedUsernameAsync(normalized, ct);
        if (user is null) return Result<ProfileDto>.Fail("General.NotFound", "Profile not found.");

        if (user.Visibility == ProfileVisibility.Private && user.Id != requesterId)
            return Result<ProfileDto>.Fail("Profile.Private", "This profile is private.");

        if (user.Visibility == ProfileVisibility.FriendsOnly && user.Id != requesterId)
            return Result<ProfileDto>.Fail("Profile.FriendsOnly", "This profile is visible to friends only.");

        return await BuildDtoAsync(user, ct);
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
        return await BuildDtoAsync(user, ct);
    }

    public Task<Result<ProfileMediaUploadUrlDto>> CreateAvatarUploadUrlAsync(
        Guid userId, ProfileMediaUploadUrlRequestDto request, CancellationToken ct = default) =>
        CreateUploadUrlAsync(userId, request, "avatar", 5 * 1024 * 1024, ct);

    public Task<Result<ProfileMediaUploadUrlDto>> CreateBannerUploadUrlAsync(
        Guid userId, ProfileMediaUploadUrlRequestDto request, CancellationToken ct = default) =>
        CreateUploadUrlAsync(userId, request, "banner", 10 * 1024 * 1024, ct);

    public Task<Result<ProfileDto>> ConfirmAvatarUploadAsync(Guid userId, string objectKey, CancellationToken ct = default) =>
        ConfirmUploadAsync(userId, objectKey, "avatar", ct);

    public Task<Result<ProfileDto>> ConfirmBannerUploadAsync(Guid userId, string objectKey, CancellationToken ct = default) =>
        ConfirmUploadAsync(userId, objectKey, "banner", ct);

    public async Task<Result<ProfileDto>> RemoveAvatarAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return Result<ProfileDto>.Fail("General.NotFound", "User not found.");

        var previousKey = user.AvatarObjectKey;
        user.ClearAvatarMedia();
        await _users.UpdateAsync(user, ct);
        if (!string.IsNullOrWhiteSpace(previousKey))
            await _storage.DeleteObjectAsync(previousKey, ct);

        return await BuildDtoAsync(user, ct);
    }

    public async Task<Result<ProfileDto>> RemoveBannerAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return Result<ProfileDto>.Fail("General.NotFound", "User not found.");

        var previousKey = user.BannerObjectKey;
        user.ClearBannerMedia();
        await _users.UpdateAsync(user, ct);
        if (!string.IsNullOrWhiteSpace(previousKey))
            await _storage.DeleteObjectAsync(previousKey, ct);

        return await BuildDtoAsync(user, ct);
    }

    public async Task<Result<ProfileDto>> UpdateAvatarFallbackColorAsync(
        Guid userId, string color, CancellationToken ct = default)
    {
        if (!IsSafeHexColor(color))
            return Result<ProfileDto>.Fail("Validation.Failed", "Avatar fallback color must be a hex color.");

        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return Result<ProfileDto>.Fail("General.NotFound", "User not found.");

        user.UpdateAvatarFallbackColor(color);
        await _users.UpdateAsync(user, ct);
        return await BuildDtoAsync(user, ct);
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

    public async Task<Result<UsernameChangeRequestDto>> RequestUsernameChangeAsync(
        Guid userId, string requestedUsername, CancellationToken ct = default)
    {
        var normalized = requestedUsername.Trim().ToUpperInvariant();

        var existing = await _usernameRequests.GetPendingByUserIdAsync(userId, ct);
        if (existing is not null)
            return Result<UsernameChangeRequestDto>.Fail("Profile.PendingRequestExists",
                "You already have a pending username change request.");

        if (await _users.ExistsByUsernameAsync(normalized, ct))
            return Result<UsernameChangeRequestDto>.Fail("Profile.UsernameTaken",
                "That username is already in use.");

        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null)
            return Result<UsernameChangeRequestDto>.Fail("General.NotFound", "User not found.");

        if (user.NormalizedUsername == normalized)
            return Result<UsernameChangeRequestDto>.Fail("Profile.SameUsername",
                "The requested username is the same as your current one.");

        var request = UsernameChangeRequest.Create(userId, requestedUsername);
        await _usernameRequests.AddAsync(request, ct);
        return Result<UsernameChangeRequestDto>.Ok(ToRequestDto(request));
    }

    public async Task<Result<UsernameChangeRequestDto?>> GetUsernameChangeRequestAsync(
        Guid userId, CancellationToken ct = default)
    {
        var request = await _usernameRequests.GetLatestByUserIdAsync(userId, ct);
        return Result<UsernameChangeRequestDto?>.Ok(request is null ? null : ToRequestDto(request));
    }

    private async Task<Result<ProfileMediaUploadUrlDto>> CreateUploadUrlAsync(
        Guid userId,
        ProfileMediaUploadUrlRequestDto request,
        string mediaKind,
        long maxBytes,
        CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return Result<ProfileMediaUploadUrlDto>.Fail("General.NotFound", "User not found.");

        if (!AllowedImageTypes.Contains(request.ContentType))
            return Result<ProfileMediaUploadUrlDto>.Fail("Validation.Failed", "Only JPEG, PNG, and WebP images are accepted.");
        if (request.FileSizeBytes <= 0 || request.FileSizeBytes > maxBytes)
            return Result<ProfileMediaUploadUrlDto>.Fail("Validation.Failed", $"{mediaKind} image exceeds the allowed size.");

        var extension = ExtensionForContentType(request.ContentType);
        if (extension is null)
            return Result<ProfileMediaUploadUrlDto>.Fail("Validation.Failed", "Unsupported image type.");

        var objectKey = $"{_aws.S3ProfilePrefix.Trim('/')}/users/{userId}/{mediaKind}/{Guid.NewGuid():N}{extension}";
        var expires = DateTime.UtcNow.AddMinutes(_aws.S3UploadUrlExpiryMinutes);
        var uploadUrl = await _storage.CreatePresignedPutUrlAsync(
            objectKey,
            request.ContentType,
            TimeSpan.FromMinutes(_aws.S3UploadUrlExpiryMinutes),
            ct);

        return Result<ProfileMediaUploadUrlDto>.Ok(new(uploadUrl, objectKey, request.ContentType, expires));
    }

    private async Task<Result<ProfileDto>> ConfirmUploadAsync(
        Guid userId,
        string objectKey,
        string mediaKind,
        CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return Result<ProfileDto>.Fail("General.NotFound", "User not found.");

        var expectedPrefix = $"{_aws.S3ProfilePrefix.Trim('/')}/users/{userId}/{mediaKind}/";
        if (!objectKey.StartsWith(expectedPrefix, StringComparison.Ordinal))
            return Result<ProfileDto>.Fail("Validation.Failed", "The uploaded object does not belong to the current user.");

        if (!await _storage.ObjectExistsAsync(objectKey, ct))
            return Result<ProfileDto>.Fail("Profile.MediaNotFound", "Uploaded media was not found in storage.");

        var previousKey = mediaKind == "avatar" ? user.AvatarObjectKey : user.BannerObjectKey;
        if (mediaKind == "avatar")
            user.SetAvatarMedia(objectKey);
        else
            user.SetBannerMedia(objectKey);

        await _users.UpdateAsync(user, ct);

        if (!string.IsNullOrWhiteSpace(previousKey) &&
            !string.Equals(previousKey, objectKey, StringComparison.Ordinal))
            await _storage.DeleteObjectAsync(previousKey, ct);

        return await BuildDtoAsync(user, ct);
    }

    private async Task<Result<ProfileDto>> BuildDtoAsync(User user, CancellationToken ct)
    {
        var links = await _profiles.GetLinksByUserIdAsync(user.Id, ct);
        var interests = await _profiles.GetInterestsByUserIdAsync(user.Id, ct);
        return Result<ProfileDto>.Ok(await ToDtoAsync(user, links, interests, ct));
    }

    private static UsernameChangeRequestDto ToRequestDto(UsernameChangeRequest r) => new(
        r.Id, r.RequestedUsername, r.Status.ToString(),
        r.RejectionReason, r.CreatedAt, r.ReviewedAt);

    private async Task<ProfileDto> ToDtoAsync(
        User user,
        IReadOnlyList<ProfileExternalLink> links,
        IReadOnlyList<ProfileInterestTag> interests,
        CancellationToken ct)
    {
        var readExpiry = TimeSpan.FromMinutes(_aws.S3ReadUrlExpiryMinutes);
        var avatarUrl = !string.IsNullOrWhiteSpace(user.AvatarObjectKey)
            ? await _storage.CreatePresignedReadUrlAsync(user.AvatarObjectKey, readExpiry, ct)
            : user.AvatarUrl;
        var bannerUrl = !string.IsNullOrWhiteSpace(user.BannerObjectKey)
            ? await _storage.CreatePresignedReadUrlAsync(user.BannerObjectKey, readExpiry, ct)
            : user.BannerUrl;

        return new ProfileDto(
            UserId: user.Id,
            Username: user.Username,
            DisplayName: user.DisplayName,
            Bio: user.Bio,
            AvatarUrl: avatarUrl,
            BannerUrl: bannerUrl,
            HasUploadedAvatar: !string.IsNullOrWhiteSpace(user.AvatarObjectKey),
            HasUploadedBanner: !string.IsNullOrWhiteSpace(user.BannerObjectKey),
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
    }

    private static ExternalLinkDto ToLinkDto(ProfileExternalLink l) => new(
        l.Id, l.Platform, l.Url, l.DisplayLabel, l.SortOrder);

    private static string? ExtensionForContentType(string contentType) =>
        contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => null
        };

    private static bool IsSafeHexColor(string color) =>
        System.Text.RegularExpressions.Regex.IsMatch(color, "^#[0-9a-fA-F]{6}$");
}
