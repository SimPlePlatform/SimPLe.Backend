using Microsoft.EntityFrameworkCore;
using SimPle.Application.Common.Interfaces;
using SimPle.Domain.Users;

namespace SimPle.Infrastructure.Persistence.Repositories;

public sealed class EmailVerificationTokenRepository : IEmailVerificationTokenRepository
{
    private readonly AppDbContext _db;

    public EmailVerificationTokenRepository(AppDbContext db) => _db = db;

    public async Task<EmailVerificationToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default) =>
        await _db.EmailVerificationTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task<EmailVerificationToken?> GetLatestByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        await _db.EmailVerificationTokens
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(EmailVerificationToken token, CancellationToken ct = default)
    {
        _db.EmailVerificationTokens.Add(token);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(EmailVerificationToken token, CancellationToken ct = default)
    {
        _db.EmailVerificationTokens.Update(token);
        await _db.SaveChangesAsync(ct);
    }

    public async Task InvalidateAllForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var tokens = await _db.EmailVerificationTokens
            .Where(t => t.UserId == userId && t.UsedAt == null)
            .ToListAsync(ct);

        foreach (var token in tokens)
            token.MarkUsed();

        await _db.SaveChangesAsync(ct);
    }

    public Task<int> DeleteExpiredAsync(DateTime before, CancellationToken ct = default) =>
        _db.EmailVerificationTokens
            .Where(t => t.ExpiresAt < before)
            .ExecuteDeleteAsync(ct);
}
