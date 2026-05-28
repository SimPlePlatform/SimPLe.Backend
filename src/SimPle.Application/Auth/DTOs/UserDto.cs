namespace SimPle.Application.Auth.DTOs;

public sealed record UserDto(
    Guid Id,
    string Username,
    string DisplayName,
    string Email,
    string Initials,
    string Color,
    string Role,
    bool IsEmailVerified,
    DateTime CreatedAt);
