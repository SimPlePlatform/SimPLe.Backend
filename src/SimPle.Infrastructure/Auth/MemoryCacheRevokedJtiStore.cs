using Microsoft.Extensions.Caching.Memory;
using SimPle.Application.Common.Interfaces;

namespace SimPle.Infrastructure.Auth;

/// <summary>
/// In-memory JTI blocklist backed by IMemoryCache.
/// Entries expire automatically after the access token's max lifetime,
/// so we never accumulate stale entries.
/// </summary>
public sealed class MemoryCacheRevokedJtiStore : IRevokedJtiStore
{
    private readonly IMemoryCache _cache;

    public MemoryCacheRevokedJtiStore(IMemoryCache cache)
    {
        _cache = cache;
    }

    public void Revoke(string jti, TimeSpan ttl)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl,
            // Use a small size value so MemoryCache size limits work correctly if configured.
            Size = 1,
        };
        _cache.Set(CacheKey(jti), true, options);
    }

    public bool IsRevoked(string jti) =>
        _cache.TryGetValue(CacheKey(jti), out _);

    private static string CacheKey(string jti) => $"revoked_jti:{jti}";
}
