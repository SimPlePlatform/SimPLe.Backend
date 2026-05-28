using Microsoft.EntityFrameworkCore;
using SimPle.Application.Common.Interfaces;
using SimPle.Domain.Users;
using SimPle.Infrastructure.Persistence;

namespace SimPle.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _db;

    public RefreshTokenRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(RefreshToken token, CancellationToken ct = default)
    {
        await _db.RefreshTokens.AddAsync(token, ct);
        await _db.SaveChangesAsync(ct);
    }

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default) =>
        _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    // Returns false if a concurrent request already updated this token (optimistic concurrency).
    // Caller should treat false as an invalid/replayed token.
    public async Task<bool> TryUpdateAsync(RefreshToken token, CancellationToken ct = default)
    {
        _db.RefreshTokens.Update(token);
        try
        {
            await _db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    public async Task UpdateAsync(RefreshToken token, CancellationToken ct = default)
    {
        _db.RefreshTokens.Update(token);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RevokeAllByFamilyIdAsync(Guid familyId, string reason, CancellationToken ct = default)
    {
        var tokens = await _db.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var t in tokens)
            t.Revoke("", reason);

        await _db.SaveChangesAsync(ct);
    }

    public async Task RevokeAllByUserIdAsync(Guid userId, string reason, CancellationToken ct = default)
    {
        var tokens = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var t in tokens)
            t.Revoke("", reason);

        await _db.SaveChangesAsync(ct);
    }

    public Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<RefreshToken>)t.Result, ct);

    public Task<RefreshToken?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.RefreshTokens.FirstOrDefaultAsync(t => t.Id == id, ct);

    // Deletes tokens that are both expired and revoked/superseded, giving a retention buffer.
    public Task<int> DeleteExpiredAsync(DateTime before, CancellationToken ct = default) =>
        _db.RefreshTokens
            .Where(t => t.ExpiresAt < before && t.RevokedAt != null)
            .ExecuteDeleteAsync(ct);
}
