using Microsoft.EntityFrameworkCore;
using SimPle.Application.Common.Interfaces;
using SimPle.Domain.Profiles;

namespace SimPle.Infrastructure.Persistence.Repositories;

public sealed class ProfileRepository : IProfileRepository
{
    private readonly AppDbContext _db;

    public ProfileRepository(AppDbContext db) => _db = db;

    public Task<IReadOnlyList<ProfileExternalLink>> GetLinksByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        _db.ProfileExternalLinks
            .Where(l => l.UserId == userId)
            .OrderBy(l => l.SortOrder)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<ProfileExternalLink>)t.Result, ct);

    public async Task ReplaceLinksAsync(Guid userId, IReadOnlyList<ProfileExternalLink> links, CancellationToken ct = default)
    {
        var existing = await _db.ProfileExternalLinks.Where(l => l.UserId == userId).ToListAsync(ct);
        _db.ProfileExternalLinks.RemoveRange(existing);
        await _db.ProfileExternalLinks.AddRangeAsync(links, ct);
        await _db.SaveChangesAsync(ct);
    }

    public Task<IReadOnlyList<ProfileInterestTag>> GetInterestsByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        _db.ProfileInterestTags
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.Name)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<ProfileInterestTag>)t.Result, ct);

    public async Task ReplaceInterestsAsync(Guid userId, IReadOnlyList<ProfileInterestTag> tags, CancellationToken ct = default)
    {
        var existing = await _db.ProfileInterestTags.Where(t => t.UserId == userId).ToListAsync(ct);
        _db.ProfileInterestTags.RemoveRange(existing);
        await _db.ProfileInterestTags.AddRangeAsync(tags, ct);
        await _db.SaveChangesAsync(ct);
    }
}
