using SimPle.Domain.Users;

namespace SimPle.Application.Common.Interfaces;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token, CancellationToken ct = default);
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);
    Task UpdateAsync(RefreshToken token, CancellationToken ct = default);
    // Returns false on optimistic concurrency conflict — treat as replayed token.
    Task<bool> TryUpdateAsync(RefreshToken token, CancellationToken ct = default);
    Task RevokeAllByFamilyIdAsync(Guid familyId, string reason, CancellationToken ct = default);
    Task RevokeAllByUserIdAsync(Guid userId, string reason, CancellationToken ct = default);
    Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<RefreshToken?> GetByIdAsync(Guid id, CancellationToken ct = default);
    // Returns number of rows deleted.
    Task<int> DeleteExpiredAsync(DateTime before, CancellationToken ct = default);
}
