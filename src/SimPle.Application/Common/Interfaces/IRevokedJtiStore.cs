namespace SimPle.Application.Common.Interfaces;

/// <summary>
/// Tracks revoked JWT IDs so access tokens can be invalidated immediately
/// when a session is revoked, without waiting for the token to expire.
/// </summary>
public interface IRevokedJtiStore
{
    void Revoke(string jti, TimeSpan ttl);
    bool IsRevoked(string jti);
}
