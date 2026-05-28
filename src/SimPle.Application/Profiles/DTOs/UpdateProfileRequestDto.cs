namespace SimPle.Application.Profiles.DTOs;

public sealed record UpdateProfileRequestDto(
    string DisplayName,
    string? Bio,
    string? AvatarUrl,
    string? BannerUrl,
    string? Region,
    string? StatusMessage,
    string? Visibility);  // "Public" | "FriendsOnly" | "Private"
