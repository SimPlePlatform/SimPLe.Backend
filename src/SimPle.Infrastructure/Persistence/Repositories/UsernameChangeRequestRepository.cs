using Microsoft.EntityFrameworkCore;
using SimPle.Application.Common.Interfaces;
using SimPle.Domain.Profiles;

namespace SimPle.Infrastructure.Persistence.Repositories;

public sealed class UsernameChangeRequestRepository : IUsernameChangeRequestRepository
{
    private readonly AppDbContext _db;

    public UsernameChangeRequestRepository(AppDbContext db) => _db = db;

    public Task<UsernameChangeRequest?> GetPendingByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        _db.UsernameChangeRequests
            .FirstOrDefaultAsync(r => r.UserId == userId && r.Status == UsernameChangeStatus.Pending, ct);

    public Task<UsernameChangeRequest?> GetLatestByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        _db.UsernameChangeRequests
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public Task<UsernameChangeRequest?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.UsernameChangeRequests.FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<IReadOnlyList<UsernameChangeRequest>> GetAllPendingAsync(CancellationToken ct = default) =>
        _db.UsernameChangeRequests
            .Where(r => r.Status == UsernameChangeStatus.Pending)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<UsernameChangeRequest>)t.Result, ct);

    public async Task AddAsync(UsernameChangeRequest request, CancellationToken ct = default)
    {
        await _db.UsernameChangeRequests.AddAsync(request, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(UsernameChangeRequest request, CancellationToken ct = default)
    {
        _db.UsernameChangeRequests.Update(request);
        await _db.SaveChangesAsync(ct);
    }
}
