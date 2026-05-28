namespace SimPle.Application.Profiles.DTOs;

// AvatarUrl and BannerUrl are intentionally excluded.
// Profile media is managed exclusively through the dedicated upload/confirm/remove endpoints.
// Accepting arbitrary URLs here would bypass presigned-upload content-type and size validation.
public sealed record UpdateProfileRequestDto(
    string DisplayName,
    string? Bio,
    string? Region,
    string? StatusMessage,
    string? Visibility,  // "Public" | "FriendsOnly" | "Private"
    string? ProfileType); // "Player" | "Developer"
