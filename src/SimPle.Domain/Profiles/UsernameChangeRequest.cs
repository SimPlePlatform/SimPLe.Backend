using SimPle.Domain.Common;

namespace SimPle.Domain.Profiles;

public class UsernameChangeRequest : Entity
{
    public Guid UserId { get; private set; }
    public string RequestedUsername { get; private set; } = default!;
    public string NormalizedRequestedUsername { get; private set; } = default!;
    public UsernameChangeStatus Status { get; private set; } = UsernameChangeStatus.Pending;
    public string? RejectionReason { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public Guid? ReviewedBy { get; private set; }

    private UsernameChangeRequest() { }

    public static UsernameChangeRequest Create(Guid userId, string requestedUsername) => new()
    {
        UserId = userId,
        RequestedUsername = requestedUsername.Trim(),
        NormalizedRequestedUsername = requestedUsername.Trim().ToUpperInvariant(),
        Status = UsernameChangeStatus.Pending,
    };

    public void Approve(Guid reviewedBy)
    {
        Status = UsernameChangeStatus.Accepted;
        ReviewedAt = DateTime.UtcNow;
        ReviewedBy = reviewedBy;
        Touch();
    }

    public void Reject(Guid reviewedBy, string? reason)
    {
        Status = UsernameChangeStatus.Rejected;
        RejectionReason = reason;
        ReviewedAt = DateTime.UtcNow;
        ReviewedBy = reviewedBy;
        Touch();
    }
}

public enum UsernameChangeStatus { Pending, Accepted, Rejected }
