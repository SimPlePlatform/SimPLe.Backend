using Microsoft.EntityFrameworkCore;
using SimPle.Application.Common.Interfaces;
using SimPle.Domain.Friends;
using SimPle.Domain.Users;

namespace SimPle.Infrastructure.Persistence.Repositories;

public sealed class FriendRepository : IFriendRepository
{
    private readonly AppDbContext _db;

    public FriendRepository(AppDbContext db) => _db = db;

    public Task<Friendship?> GetFriendshipAsync(Guid userId, Guid friendUserId, CancellationToken ct = default) =>
        _db.Friendships.FirstOrDefaultAsync(
            f => (f.UserId == userId && f.FriendUserId == friendUserId) ||
                 (f.UserId == friendUserId && f.FriendUserId == userId), ct);

    public Task<bool> AreFriendsAsync(Guid userId, Guid friendUserId, CancellationToken ct = default) =>
        _db.Friendships.AnyAsync(
            f => (f.UserId == userId && f.FriendUserId == friendUserId) ||
                 (f.UserId == friendUserId && f.FriendUserId == userId), ct);

    public Task<int> CountFriendsAsync(Guid userId, CancellationToken ct = default) =>
        _db.Friendships.CountAsync(f => f.UserId == userId || f.FriendUserId == userId, ct);

    public async Task<int> CountMutualFriendsAsync(Guid userId, Guid otherUserId, CancellationToken ct = default)
    {
        if (userId == otherUserId) return 0;
        var userFriends = FriendIdsQuery(userId);
        var otherFriends = FriendIdsQuery(otherUserId);
        return await userFriends.Intersect(otherFriends).CountAsync(ct);
    }

    public async Task<IReadOnlyList<User>> GetFriendsAsync(Guid userId, CancellationToken ct = default)
    {
        var ids = await FriendIdsQuery(userId).ToListAsync(ct);
        return await _db.Users
            .Where(u => ids.Contains(u.Id))
            .OrderBy(u => u.DisplayName)
            .ToListAsync(ct);
    }

    public Task<FriendRequest?> GetRequestAsync(Guid requestId, CancellationToken ct = default) =>
        _db.FriendRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct);

    public Task<FriendRequest?> GetPendingRequestBetweenAsync(Guid firstUserId, Guid secondUserId, CancellationToken ct = default) =>
        _db.FriendRequests.FirstOrDefaultAsync(r =>
            r.Status == FriendRequestStatus.Pending &&
            ((r.SenderUserId == firstUserId && r.ReceiverUserId == secondUserId) ||
             (r.SenderUserId == secondUserId && r.ReceiverUserId == firstUserId)), ct);

    public async Task<IReadOnlyList<FriendRequest>> GetIncomingRequestsAsync(Guid userId, CancellationToken ct = default) =>
        await _db.FriendRequests
            .Where(r => r.ReceiverUserId == userId && r.Status == FriendRequestStatus.Pending)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<FriendRequest>> GetOutgoingRequestsAsync(Guid userId, CancellationToken ct = default) =>
        await _db.FriendRequests
            .Where(r => r.SenderUserId == userId && r.Status == FriendRequestStatus.Pending)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

    public async Task AddRequestAsync(FriendRequest request, CancellationToken ct = default)
    {
        await _db.FriendRequests.AddAsync(request, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateRequestAsync(FriendRequest request, CancellationToken ct = default)
    {
        _db.FriendRequests.Update(request);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddFriendshipAsync(Friendship friendship, CancellationToken ct = default)
    {
        await _db.Friendships.AddAsync(friendship, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveFriendshipAsync(Guid userId, Guid friendUserId, CancellationToken ct = default)
    {
        var friendship = await GetFriendshipAsync(userId, friendUserId, ct);
        if (friendship is not null)
            _db.Friendships.Remove(friendship);
        await _db.SaveChangesAsync(ct);
    }

    public Task<UserBlock?> GetBlockAsync(Guid blockerUserId, Guid blockedUserId, CancellationToken ct = default) =>
        _db.UserBlocks.FirstOrDefaultAsync(b => b.BlockerUserId == blockerUserId && b.BlockedUserId == blockedUserId, ct);

    public Task<bool> IsBlockedEitherDirectionAsync(Guid firstUserId, Guid secondUserId, CancellationToken ct = default) =>
        _db.UserBlocks.AnyAsync(b =>
            (b.BlockerUserId == firstUserId && b.BlockedUserId == secondUserId) ||
            (b.BlockerUserId == secondUserId && b.BlockedUserId == firstUserId), ct);

    public async Task<IReadOnlyList<UserBlock>> GetBlocksAsync(Guid blockerUserId, CancellationToken ct = default) =>
        await _db.UserBlocks
            .Where(b => b.BlockerUserId == blockerUserId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(ct);

    public async Task AddBlockAsync(UserBlock block, CancellationToken ct = default)
    {
        await _db.UserBlocks.AddAsync(block, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveBlockAsync(Guid blockerUserId, Guid blockedUserId, CancellationToken ct = default)
    {
        var block = await GetBlockAsync(blockerUserId, blockedUserId, ct);
        if (block is not null)
            _db.UserBlocks.Remove(block);
        await _db.SaveChangesAsync(ct);
    }

    public async Task ClearRelationshipAsync(Guid firstUserId, Guid secondUserId, CancellationToken ct = default)
    {
        var friendship = await GetFriendshipAsync(firstUserId, secondUserId, ct);
        if (friendship is not null) _db.Friendships.Remove(friendship);

        var requests = await _db.FriendRequests
            .Where(r => r.Status == FriendRequestStatus.Pending &&
                ((r.SenderUserId == firstUserId && r.ReceiverUserId == secondUserId) ||
                 (r.SenderUserId == secondUserId && r.ReceiverUserId == firstUserId)))
            .ToListAsync(ct);
        foreach (var request in requests)
            request.Cancel();

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<User>> SearchUsersAsync(Guid requesterId, string query, int limit, CancellationToken ct = default)
    {
        var normalized = query.Trim().ToUpperInvariant();
        var excluded = ExcludedUserIdsQuery(requesterId);

        return await _db.Users
            .Where(u => u.Id != requesterId &&
                !excluded.Contains(u.Id) &&
                (u.NormalizedUsername.Contains(normalized) || u.DisplayName.ToUpper().Contains(normalized)))
            .OrderBy(u => u.DisplayName)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<User>> GetSuggestionsAsync(Guid requesterId, int limit, CancellationToken ct = default)
    {
        var excluded = ExcludedUserIdsQuery(requesterId);
        var suggestions = await _db.Users
            .Where(u => u.Id != requesterId && !excluded.Contains(u.Id))
            .OrderBy(u => u.DisplayName)
            .Take(limit * 2)
            .ToListAsync(ct);

        var ranked = new List<(User User, int Mutuals)>();
        foreach (var user in suggestions)
            ranked.Add((user, await CountMutualFriendsAsync(requesterId, user.Id, ct)));

        return ranked
            .OrderByDescending(x => x.Mutuals)
            .ThenBy(x => x.User.DisplayName)
            .Take(limit)
            .Select(x => x.User)
            .ToList();
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);

    private IQueryable<Guid> FriendIdsQuery(Guid userId) =>
        _db.Friendships
            .Where(f => f.UserId == userId || f.FriendUserId == userId)
            .Select(f => f.UserId == userId ? f.FriendUserId : f.UserId);

    private IQueryable<Guid> ExcludedUserIdsQuery(Guid requesterId)
    {
        var friendIds = FriendIdsQuery(requesterId);
        var pendingIds = _db.FriendRequests
            .Where(r => r.Status == FriendRequestStatus.Pending &&
                (r.SenderUserId == requesterId || r.ReceiverUserId == requesterId))
            .Select(r => r.SenderUserId == requesterId ? r.ReceiverUserId : r.SenderUserId);
        var blockedIds = _db.UserBlocks
            .Where(b => b.BlockerUserId == requesterId || b.BlockedUserId == requesterId)
            .Select(b => b.BlockerUserId == requesterId ? b.BlockedUserId : b.BlockerUserId);

        return friendIds.Concat(pendingIds).Concat(blockedIds);
    }
}
