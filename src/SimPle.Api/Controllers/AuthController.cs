using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using SimPle.Api.Models;
using SimPle.Application.Auth.DTOs;
using SimPle.Application.Auth.Services;
using SimPle.Application.Common.Interfaces;
using SimPle.Application.Common.Options;
using SimPle.Shared.Common;
using Swashbuckle.AspNetCore.Annotations;

namespace SimPle.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
public sealed class AuthController : ControllerBase
{
    private const string AccessTokenCookie = "access_token";
    private const string RefreshTokenCookie = "refresh_token";
    private const string CsrfHeader = "X-Requested-With";
    private const string CsrfHeaderValue = "XMLHttpRequest";

    private readonly IAuthService _authService;
    private readonly ICaptchaVerificationService _captchaVerification;
    private readonly IWebHostEnvironment _environment;
    private readonly AuthOptions _authOptions;

    public AuthController(
        IAuthService authService,
        ICaptchaVerificationService captchaVerification,
        IWebHostEnvironment environment,
        IOptions<AuthOptions> authOptions)
    {
        _authService = authService;
        _captchaVerification = captchaVerification;
        _environment = environment;
        _authOptions = authOptions.Value;
    }

    // ── Registration ──────────────────────────────────────────────────────────

    [HttpGet("check-email")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-check-email")]
    [SwaggerOperation(
        Summary = "Check whether an email address is available",
        Description =
            "Returns **204 No Content** if the address is not yet registered, or **409 Conflict** if it is taken. " +
            "Designed for real-time inline validation on the registration form — call on blur, not on every keystroke. " +
            "Rate-limited to **20 requests per minute** per IP.",
        OperationId = "Auth_CheckEmail",
        Tags = new[] { "Registration" })]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> CheckEmail(
        [FromQuery] string email,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(ErrorResponse("Validation.Required", "Email is required."));

        var taken = await _authService.EmailExistsAsync(email, ct);
        return taken
            ? Conflict(ErrorResponse("Auth.EmailTaken", "An account with this email already exists."))
            : NoContent();
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-register")]
    [SwaggerOperation(
        Summary = "Create a new user account",
        Description =
            "Registers a new user, signs them in immediately, and sends an email verification link. " +
            "On success, two **HttpOnly cookies** are set: `access_token` (15 min JWT) and `refresh_token` (7 days). " +
            "Tokens never appear in the response body. " +
            "\n\n**Requirements:**\n" +
            "- `X-Requested-With: XMLHttpRequest` header (CSRF protection).\n" +
            "- Valid Google reCAPTCHA v2 token in the request body.\n" +
            "- Username must be unique; email must be unique.\n" +
            "- Password: min 8 chars, at least 2 character types, not a common password.\n" +
            "\n**Rate limit:** 3 requests per minute per IP.",
        OperationId = "Auth_Register",
        Tags = new[] { "Registration" })]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Register(RegisterRequestDto request, CancellationToken ct)
    {
        if (!HasCsrfHeader())
            return MissingCsrfHeader();

        if (!await CaptchaIsValid(request.CaptchaToken, ct))
            return InvalidCaptcha();

        var result = await _authService.RegisterAsync(
            request,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            Request.Headers.UserAgent.ToString(),
            ct);

        if (!result.IsSuccess)
            return Failure(result.Error);

        SetAuthCookies(result.Value!);
        return StatusCode(StatusCodes.Status201Created, result.Value!.User);
    }

    // ── Session ───────────────────────────────────────────────────────────────

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-login")]
    [SwaggerOperation(
        Summary = "Sign in with email or username",
        Description =
            "Validates credentials and issues an `access_token` and `refresh_token` as **HttpOnly cookies**. " +
            "Neither token is included in the JSON response body. " +
            "\n\n**Requirements:**\n" +
            "- `X-Requested-With: XMLHttpRequest` header (CSRF protection).\n" +
            "- Valid Google reCAPTCHA v2 token in the request body.\n" +
            "\n**Security features:**\n" +
            "- Accounts are locked for **15 minutes** after 10 consecutive failed attempts (configurable).\n" +
            "- Locked-out accounts return `401` with code `Auth.LockedOut`.\n" +
            "\n**Rate limit:** 5 requests per minute per IP.",
        OperationId = "Auth_Login",
        Tags = new[] { "Session" })]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(LoginRequestDto request, CancellationToken ct)
    {
        if (!HasCsrfHeader())
            return MissingCsrfHeader();

        if (!await CaptchaIsValid(request.CaptchaToken, ct))
            return InvalidCaptcha();

        var result = await _authService.LoginAsync(
            request,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            Request.Headers.UserAgent.ToString(),
            ct);

        if (!result.IsSuccess)
            return Failure(result.Error);

        SetAuthCookies(result.Value!);
        return Ok(result.Value!.User);
    }

    [HttpGet("me")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Get the currently authenticated user",
        Description =
            "Safe authenticated-user response. " +
            "Returns the safe user DTO for the identity encoded in the `access_token` cookie. " +
            "Use this on app load to restore session state without storing user data locally. " +
            "\n\n**Requires:** valid `access_token` cookie.",
        OperationId = "Auth_Me",
        Tags = new[] { "Session" })]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await _authService.GetCurrentUserAsync(userId, ct);
        return result.IsSuccess ? Ok(result.Value) : Failure(result.Error);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-refresh")]
    [SwaggerOperation(
        Summary = "Rotate the session — exchange a refresh token for a new token pair",
        Description =
            "Consumes the `refresh_token` cookie and issues a fresh `access_token` + `refresh_token` pair. " +
            "The old refresh token is immediately revoked. " +
            "\n\n**Reuse detection:** if a previously rotated (already consumed) token is presented, " +
            "the **entire session family** is revoked, forcing re-authentication. " +
            "This detects token theft where an attacker replays a stolen refresh token. " +
            "\n\n**Requirements:**\n" +
            "- `X-Requested-With: XMLHttpRequest` header.\n" +
            "- Valid `refresh_token` cookie.\n" +
            "\n**Rate limit:** 10 requests per minute per IP.",
        OperationId = "Auth_Refresh",
        Tags = new[] { "Session" })]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        if (!HasCsrfHeader())
            return MissingCsrfHeader();

        if (!Request.Cookies.TryGetValue(RefreshTokenCookie, out var refreshToken))
            return UnauthorizedError("Auth.InvalidToken", "Refresh token is invalid or has expired.");

        var result = await _authService.RefreshAsync(
            refreshToken,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            ct);

        if (!result.IsSuccess)
        {
            ClearAuthCookies();
            return Failure(result.Error);
        }

        SetAuthCookies(result.Value!);
        return Ok(result.Value!.User);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Sign out of the current session",
        Description =
            "Revokes the current `refresh_token` (if present) and clears both auth cookies. " +
            "Safe to call even when not signed in — always returns 204. " +
            "\n\n**Requirements:** `X-Requested-With: XMLHttpRequest` header.",
        OperationId = "Auth_Logout",
        Tags = new[] { "Session" })]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        if (!HasCsrfHeader())
            return MissingCsrfHeader();

        if (Request.Cookies.TryGetValue(RefreshTokenCookie, out var refreshToken))
            await _authService.LogoutAsync(refreshToken, ct);

        ClearAuthCookies();
        return NoContent();
    }

    [HttpPost("logout-all")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Sign out from every active session",
        Description =
            "Revokes **all** refresh tokens for the authenticated user and clears the current cookies. " +
            "Use this for a security-conscious sign-out that ends every concurrent session (other browsers, devices, etc.). " +
            "\n\n**Requirements:**\n" +
            "- `X-Requested-With: XMLHttpRequest` header.\n" +
            "- Valid `access_token` cookie.",
        OperationId = "Auth_LogoutAll",
        Tags = new[] { "Session" })]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LogoutAll(CancellationToken ct)
    {
        if (!HasCsrfHeader())
            return MissingCsrfHeader();

        if (!TryGetUserId(out var userId))
            return Unauthorized();

        await _authService.LogoutAllAsync(userId, ct);
        ClearAuthCookies();
        return NoContent();
    }

    // ── Email Verification ────────────────────────────────────────────────────

    [HttpPost("resend-verification")]
    [Authorize]
    [EnableRateLimiting("auth-resend-verification")]
    [SwaggerOperation(
        Summary = "Resend the email verification link",
        Description =
            "Sends a new verification email to the authenticated user's address. " +
            "Returns 204 whether or not the user is already verified (idempotent). " +
            "\n\n**Requirements:**\n" +
            "- `X-Requested-With: XMLHttpRequest` header.\n" +
            "- Valid `access_token` cookie.\n" +
            "\n**Rate limit:** 3 requests per 5 minutes per IP with a 60-second per-user cooldown.",
        OperationId = "Auth_ResendVerification",
        Tags = new[] { "Email Verification" })]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ResendVerification(CancellationToken ct)
    {
        if (!HasCsrfHeader())
            return MissingCsrfHeader();

        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await _authService.SendVerificationEmailAsync(userId, ct);

        if (!result.IsSuccess)
            return result.Error?.Code == "Auth.AlreadyVerified"
                ? NoContent()
                : BadRequest(ErrorResponse(result.Error!.Code, result.Error.Message));

        return NoContent();
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Verify an email address with a one-time token",
        Description =
            "Marks the account's email as verified using the single-use token from the verification email. " +
            "The token is embedded as a `?token=` query parameter in the link; pass its raw value here. " +
            "\n\n**Token properties:**\n" +
            "- Single-use: verified tokens cannot be resubmitted.\n" +
            "- Expires after **24 hours**.\n" +
            "- Stored as an Argon2id hash; the raw value is never persisted.\n" +
            "\n**Requirements:** `X-Requested-With: XMLHttpRequest` header.",
        OperationId = "Auth_VerifyEmail",
        Tags = new[] { "Email Verification" })]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequestDto request, CancellationToken ct)
    {
        if (!HasCsrfHeader())
            return MissingCsrfHeader();

        var result = await _authService.VerifyEmailAsync(request.Token, ct);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse(result.Error!.Code, result.Error.Message));
    }

    // ── Password Reset ────────────────────────────────────────────────────────

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-forgot-password")]
    [SwaggerOperation(
        Summary = "Request a password reset link",
        Description =
            "Sends a password reset email to the given address if it belongs to a registered account. " +
            "**Always returns 204** — no indication is given of whether the email exists (prevents account enumeration). " +
            "The reset link expires after **1 hour**. " +
            "\n\n**Requirements:**\n" +
            "- `X-Requested-With: XMLHttpRequest` header.\n" +
            "\n**Rate limit:** 3 requests per 10 minutes per IP.",
        OperationId = "Auth_ForgotPassword",
        Tags = new[] { "Password Reset" })]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequestDto request, CancellationToken ct)
    {
        if (!HasCsrfHeader())
            return MissingCsrfHeader();

        await _authService.ForgotPasswordAsync(request.Email, ct);
        return NoContent();
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-reset-password")]
    [SwaggerOperation(
        Summary = "Reset a password using a token from the reset email",
        Description =
            "Validates the reset token, updates the password to the new value, " +
            "revokes all existing refresh sessions, and sends a security notification email. " +
            "\n\n**Token properties:**\n" +
            "- Single-use: tokens are invalidated immediately on first use.\n" +
            "- Expires after **1 hour**.\n" +
            "- Stored as an Argon2id hash; the raw value is never persisted.\n" +
            "\n**Post-reset:** all sessions are revoked. The user must sign in again with the new password. " +
            "\n\n**Requirements:**\n" +
            "- `X-Requested-With: XMLHttpRequest` header.\n" +
            "\n**Rate limit:** 5 requests per 10 minutes per IP.",
        OperationId = "Auth_ResetPassword",
        Tags = new[] { "Password Reset" })]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequestDto request, CancellationToken ct)
    {
        if (!HasCsrfHeader())
            return MissingCsrfHeader();

        var result = await _authService.ResetPasswordAsync(request.Token, request.NewPassword, ct);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse(result.Error!.Code, result.Error.Message));
    }

    // ── Social Login ──────────────────────────────────────────────────────────

    [HttpPost("google")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-google")]
    [SwaggerOperation(
        Summary = "Sign in or register with a Google account",
        Description =
            "Accepts the Google ID token issued by Google Identity Services after the user consents. " +
            "Validates the JWT signature against Google's public JWKS keys, checks the `aud` claim, and issues " +
            "the same `access_token` + `refresh_token` HttpOnly cookie pair as the regular login flow. " +
            "\n\n**Account resolution (in order):**\n" +
            "1. Existing account already linked to this Google ID → signed in directly.\n" +
            "2. Existing password account with the same email → Google ID silently linked, then signed in.\n" +
            "3. No matching account → new user provisioned; username derived from Google given name " +
            "(random digits appended if taken). Email is pre-verified.\n" +
            "\n\n**Security:**\n" +
            "- Token validated server-side — no trust placed on client-supplied profile data.\n" +
            "- The OAuth client secret is **never used or stored** in this flow.\n" +
            "- Raw Google credentials are never persisted.\n" +
            "\n**Requirements:**\n" +
            "- `X-Requested-With: XMLHttpRequest` header (CSRF protection).\n" +
            "\n**Rate limit:** 10 requests per minute per IP.",
        OperationId = "Auth_Google",
        Tags = new[] { "Social Login" })]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleCallbackRequestDto request, CancellationToken ct)
    {
        if (!HasCsrfHeader())
            return MissingCsrfHeader();

        var result = await _authService.GoogleLoginAsync(
            request.IdToken,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            Request.Headers.UserAgent.ToString(),
            ct);

        if (!result.IsSuccess)
            return Failure(result.Error);

        SetAuthCookies(result.Value!);
        return Ok(result.Value!.User);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetAuthCookies(AuthTokenResult result)
    {
        var secure = !_environment.IsDevelopment();
        var sharedOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSiteMode.Lax,
            IsEssential = true
        };

        Response.Cookies.Append(AccessTokenCookie, result.AccessToken, new CookieOptions(sharedOptions)
        {
            Path = "/",
            Expires = new DateTimeOffset(result.AccessExpiresAt),
            MaxAge = result.AccessExpiresAt - DateTime.UtcNow
        });
        Response.Cookies.Append(RefreshTokenCookie, result.RawRefreshToken, new CookieOptions(sharedOptions)
        {
            Path = "/api/auth",
            Expires = DateTimeOffset.UtcNow.AddDays(_authOptions.RefreshTokenExpiryDays),
            MaxAge = TimeSpan.FromDays(_authOptions.RefreshTokenExpiryDays)
        });
    }

    private void ClearAuthCookies()
    {
        var secure = !_environment.IsDevelopment();
        Response.Cookies.Delete(AccessTokenCookie, new CookieOptions
        {
            Path = "/",
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSiteMode.Lax
        });
        Response.Cookies.Delete(RefreshTokenCookie, new CookieOptions
        {
            Path = "/api/auth",
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSiteMode.Lax
        });
    }

    private bool HasCsrfHeader() =>
        string.Equals(Request.Headers[CsrfHeader], CsrfHeaderValue, StringComparison.Ordinal);

    private IActionResult MissingCsrfHeader() => BadRequest(ErrorResponse(
        "Auth.CsrfHeaderRequired",
        $"The {CsrfHeader} header is required for this request."));

    private Task<bool> CaptchaIsValid(string responseToken, CancellationToken ct) =>
        _captchaVerification.VerifyAsync(
            responseToken,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            ct);

    private IActionResult InvalidCaptcha() => BadRequest(ErrorResponse(
        "Auth.CaptchaFailed",
        "Please complete the CAPTCHA check and try again."));

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out userId);

    private IActionResult Failure(Error? error)
    {
        var actual = error ?? new Error("General.Error", "The request could not be completed.");
        return actual.Code switch
        {
            "Auth.EmailTaken" or "Auth.UsernameTaken" => ConflictError(actual),
            "Auth.InvalidCredentials" or "Auth.InvalidToken" or "Auth.TokenReuse" or "Auth.LockedOut"
                or "Auth.InvalidGoogleToken" or "Auth.Suspended" =>
                UnauthorizedError(actual.Code, actual.Message),
            "General.NotFound" => NotFound(ErrorResponse(actual.Code, actual.Message)),
            _ => BadRequest(ErrorResponse(actual.Code, actual.Message))
        };
    }

    private IActionResult ConflictError(Error error) =>
        Conflict(ErrorResponse(error.Code, error.Message));

    private IActionResult UnauthorizedError(string code, string message) =>
        Unauthorized(ErrorResponse(code, message));

    private static ApiErrorResponse ErrorResponse(string code, string message) =>
        new(new ApiErrorDetail(code, message));
}
