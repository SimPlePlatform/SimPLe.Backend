namespace SimPle.Application.Friends.DTOs;

public sealed record UserSummaryDto(
    Guid UserId,
    string Username,
    string DisplayName,
    string? AvatarUrl,
    string AvatarFallbackColor,
    string Initials,
    string ProfileType,
    int MutualFriendsCount,
    string FriendshipStatus);

public sealed record FriendRequestDto(
    Guid Id,
    UserSummaryDto User,
    string Direction,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? RespondedAtUtc,
    DateTime? CancelledAtUtc);

public sealed record FriendRequestsDto(
    IReadOnlyList<FriendRequestDto> Incoming,
    IReadOnlyList<FriendRequestDto> Outgoing);

public sealed record BlockedUserDto(
    Guid UserId,
    string Username,
    string DisplayName,
    string? AvatarUrl,
    string AvatarFallbackColor,
    string Initials,
    DateTime BlockedAtUtc);

public sealed record SendFriendRequestDto(Guid UserId);
public sealed record BlockUserRequestDto(Guid UserId);
public sealed record UpdateFriendPrivacyDto(string FriendRequestPolicy);
public sealed record FriendPrivacyDto(string FriendRequestPolicy);
