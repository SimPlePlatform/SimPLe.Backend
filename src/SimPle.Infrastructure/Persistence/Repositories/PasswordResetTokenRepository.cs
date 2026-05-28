using Microsoft.EntityFrameworkCore;
using SimPle.Application.Common.Interfaces;
using SimPle.Domain.Users;

namespace SimPle.Infrastructure.Persistence.Repositories;

public sealed class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly AppDbContext _db;

    public PasswordResetTokenRepository(AppDbContext db) => _db = db;

    public async Task<PasswordResetToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default) =>
        await _db.PasswordResetTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task AddAsync(PasswordResetToken token, CancellationToken ct = default)
    {
        _db.PasswordResetTokens.Add(token);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(PasswordResetToken token, CancellationToken ct = default)
    {
        _db.PasswordResetTokens.Update(token);
        await _db.SaveChangesAsync(ct);
    }

    public async Task InvalidateAllForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var tokens = await _db.PasswordResetTokens
            .Where(t => t.UserId == userId && t.UsedAt == null)
            .ToListAsync(ct);

        foreach (var token in tokens)
            token.MarkUsed();

        await _db.SaveChangesAsync(ct);
    }

    public Task<int> DeleteExpiredAsync(DateTime before, CancellationToken ct = default) =>
        _db.PasswordResetTokens
            .Where(t => t.ExpiresAt < before)
            .ExecuteDeleteAsync(ct);
}
