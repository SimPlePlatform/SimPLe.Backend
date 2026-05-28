using SimPle.Application.Auth.DTOs;

namespace SimPle.Application.Common.Interfaces;

/// <summary>
/// Validates a Google ID token and returns the verified user info,
/// or <c>null</c> if the token is invalid, expired, or issued to a different audience.
/// </summary>
public interface IGoogleTokenValidationService
{
    Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken ct = default);
}
