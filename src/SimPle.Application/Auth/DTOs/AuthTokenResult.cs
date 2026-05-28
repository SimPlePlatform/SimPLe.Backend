namespace SimPle.Application.Auth.DTOs;

// Returned by login and refresh. The controller puts AccessToken and RawRefreshToken into
// HttpOnly cookies — they never appear in the JSON response body.
public sealed record AuthTokenResult(
    string AccessToken,
    string RawRefreshToken,
    DateTime AccessExpiresAt,
    UserDto User);
