using SimPle.Domain.Common;

namespace SimPle.Domain.Users;

public class RefreshToken : Entity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = default!;

    // All tokens in the same login session share a FamilyId.
    // If a revoked token from a family is presented, we revoke the entire family (reuse detection).
    public Guid FamilyId { get; private set; }

    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? RevokedReason { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }
    public string CreatedByIp { get; private set; } = default!;
    public string? UserAgent { get; private set; }

    public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpiresAt;
    public bool IsRevoked => RevokedAt != null;

    private RefreshToken() { }

    public static RefreshToken Create(
        Guid userId,
        string tokenHash,
        Guid familyId,
        DateTime expiresAt,
        string createdByIp,
        string? userAgent)
    {
        return new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            FamilyId = familyId,
            ExpiresAt = expiresAt,
            CreatedByIp = createdByIp,
            UserAgent = userAgent,
        };
    }

    public void Revoke(string byIp, string reason, string? replacedByHash = null)
    {
        RevokedAt = DateTime.UtcNow;
        RevokedReason = reason;
        ReplacedByTokenHash = replacedByHash;
        Touch();
    }
}
