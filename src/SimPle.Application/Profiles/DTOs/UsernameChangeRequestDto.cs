namespace SimPle.Application.Profiles.DTOs;

public sealed record UsernameChangeRequestDto(
    Guid Id,
    string RequestedUsername,
    string Status,           // "Pending" | "Accepted" | "Rejected" | "Cancelled"
    string? RejectionReason,
    int RequestYear,
    int RequestMonth,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? CancelledAt,
    DateTime? ReviewedAt,
    bool CanEdit,
    bool CanCancel);

public sealed record UsernameChangeResultDto(
    bool AppliedImmediately,
    string Message,
    UsernameChangeRequestDto? Request);
