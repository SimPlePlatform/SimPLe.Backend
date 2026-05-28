using SimPle.Application.Auth.DTOs;
using SimPle.Shared.Common;

namespace SimPle.Application.Auth.Services;

public interface IAuthService
{
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task<Result<AuthTokenResult>> GoogleLoginAsync(string idToken, string ipAddress, string? userAgent, CancellationToken ct = default);
    Task<Result<AuthTokenResult>> RegisterAsync(RegisterRequestDto request, string ipAddress, string? userAgent, CancellationToken ct = default);
    Task<Result<AuthTokenResult>> LoginAsync(LoginRequestDto request, string ipAddress, string? userAgent, CancellationToken ct = default);
    Task<Result<AuthTokenResult>> RefreshAsync(string rawRefreshToken, string ipAddress, CancellationToken ct = default);
    Task<Result> LogoutAsync(string rawRefreshToken, CancellationToken ct = default);
    Task<Result> LogoutAllAsync(Guid userId, CancellationToken ct = default);
    Task<Result<UserDto>> GetCurrentUserAsync(Guid userId, CancellationToken ct = default);

    Task<Result> SendVerificationEmailAsync(Guid userId, CancellationToken ct = default);
    Task<Result> VerifyEmailAsync(string rawToken, CancellationToken ct = default);
    Task<Result> ForgotPasswordAsync(string email, CancellationToken ct = default);
    Task<Result> ResetPasswordAsync(string rawToken, string newPassword, CancellationToken ct = default);

    Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct = default);
    Task<Result> RequestEmailChangeAsync(Guid userId, string newEmail, CancellationToken ct = default);
    Task<Result<IReadOnlyList<SessionDto>>> GetActiveSessionsAsync(Guid userId, string? rawRefreshToken, CancellationToken ct = default);
    Task<Result> RevokeSessionAsync(Guid userId, Guid sessionId, CancellationToken ct = default);
    Task<Result> DeleteAccountAsync(Guid userId, string password, CancellationToken ct = default);
}
