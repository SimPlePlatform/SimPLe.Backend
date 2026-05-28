using SimPle.Domain.Common;

namespace SimPle.Domain.Users;

public class PasswordResetToken : Entity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = default!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? UsedAt { get; private set; }

    public bool IsValid => UsedAt == null && DateTime.UtcNow < ExpiresAt;

    private PasswordResetToken() { }

    public static PasswordResetToken Create(Guid userId, string tokenHash)
    {
        return new PasswordResetToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
        };
    }

    public void MarkUsed()
    {
        UsedAt = DateTime.UtcNow;
        Touch();
    }
}
