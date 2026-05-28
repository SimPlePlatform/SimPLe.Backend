using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using SimPle.Application.Auth.DTOs;
using SimPle.Application.Common.Interfaces;
using SimPle.Application.Common.Options;

namespace SimPle.Infrastructure.Auth;

/// <summary>
/// Validates Google ID tokens using Google's public keys (fetched from Google's JWKS endpoint).
/// The <c>aud</c> claim is checked against the configured <see cref="GoogleOptions.ClientId"/>.
/// </summary>
public sealed class GoogleTokenValidationService : IGoogleTokenValidationService
{
    private readonly GoogleOptions _options;

    public GoogleTokenValidationService(IOptions<GoogleOptions> options)
    {
        _options = options.Value;
    }

    public async Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken ct = default)
    {
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _options.ClientId },
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

            return new GoogleUserInfo(
                GoogleId: payload.Subject,
                Email: payload.Email,
                DisplayName: payload.Name ?? payload.Email,
                GivenName: payload.GivenName,
                PictureUrl: payload.Picture,
                EmailVerified: payload.EmailVerified);
        }
        catch (Exception) when (true)
        {
            // InvalidJwtException  → bad/expired token
            // HttpRequestException → can't reach Google's JWKS endpoint
            // Any other exception  → treat as invalid; never let validation errors become 500s
            return null;
        }
    }
}
