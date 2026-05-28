using Microsoft.EntityFrameworkCore;
using SimPle.Application.Common.Interfaces;
using SimPle.Domain.Users;
using SimPle.Infrastructure.Persistence;

namespace SimPle.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db) => _db = db;

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByGoogleIdAsync(string googleId, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId, ct);

    public Task<User?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, ct);

    public Task<User?> GetByNormalizedUsernameAsync(string normalizedUsername, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.NormalizedUsername == normalizedUsername, ct);

    public Task<User?> GetByNormalizedEmailOrUsernameAsync(string normalized, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(
            u => u.NormalizedEmail == normalized || u.NormalizedUsername == normalized, ct);

    public Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken ct = default) =>
        _db.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail, ct);

    public Task<bool> ExistsByUsernameAsync(string normalizedUsername, CancellationToken ct = default) =>
        _db.Users.AnyAsync(u => u.NormalizedUsername == normalizedUsername, ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        await _db.Users.AddAsync(user, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Update(user);
        await _db.SaveChangesAsync(ct);
    }
}
