using SimPle.Domain.Common;

namespace SimPle.Domain.Users;

public class EmailVerificationToken : Entity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = default!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? UsedAt { get; private set; }
    /// <summary>Non-null when the token is confirming an email-address change rather than initial verification.</summary>
    public string? PendingEmail { get; private set; }

    public bool IsValid => UsedAt == null && DateTime.UtcNow < ExpiresAt;

    private EmailVerificationToken() { }

    public static EmailVerificationToken Create(Guid userId, string tokenHash, string? pendingEmail = null)
    {
        return new EmailVerificationToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            PendingEmail = pendingEmail,
        };
    }

    public void MarkUsed()
    {
        UsedAt = DateTime.UtcNow;
        Touch();
    }
}
