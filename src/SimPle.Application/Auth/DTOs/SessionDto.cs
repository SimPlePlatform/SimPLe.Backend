namespace SimPle.Application.Auth.DTOs;

public sealed record SessionDto(
    Guid Id,
    string IpAddress,
    string? UserAgent,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    bool IsCurrent);
