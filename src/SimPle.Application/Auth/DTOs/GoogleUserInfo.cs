namespace SimPle.Application.Auth.DTOs;

/// <summary>Validated claims extracted from a Google ID token.</summary>
public sealed record GoogleUserInfo(
    string GoogleId,
    string Email,
    string DisplayName,
    string? GivenName,
    string? PictureUrl,
    bool EmailVerified);
