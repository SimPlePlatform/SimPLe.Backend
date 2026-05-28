namespace SimPle.Application.Profiles.DTOs;

public sealed record UsernameChangeRequestDto(
    Guid Id,
    string RequestedUsername,
    string Status,           // "Pending" | "Accepted" | "Rejected"
    string? RejectionReason,
    DateTime CreatedAt,
    DateTime? ReviewedAt);
