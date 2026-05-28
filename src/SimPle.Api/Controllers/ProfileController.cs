using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimPle.Api.Models;
using SimPle.Application.Profiles.DTOs;
using SimPle.Application.Profiles.Services;
using SimPle.Application.Profiles.Validators;
using Swashbuckle.AspNetCore.Annotations;

namespace SimPle.Api.Controllers;

[ApiController]
[Route("api/profile")]
[Produces("application/json")]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
public sealed class ProfileController : ControllerBase
{
    private readonly IProfileService _profile;

    public ProfileController(IProfileService profile) => _profile = profile;

    // ── Current user profile ──────────────────────────────────────────────────

    [HttpGet("me")]
    [Authorize]
    [SwaggerOperation(Summary = "Get the authenticated user's full profile",
        OperationId = "Profile_GetMe", Tags = new[] { "Profile" })]
    [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyProfile(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _profile.GetMyProfileAsync(userId, ct);
        if (!result.IsSuccess) return NotFound(Error(result.Error!.Code, result.Error.Message));
        return Ok(result.Value);
    }

    [HttpPut("me")]
    [Authorize]
    [SwaggerOperation(Summary = "Update the authenticated user's profile",
        OperationId = "Profile_UpdateMe", Tags = new[] { "Profile" })]
    [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequestDto request, CancellationToken ct)
    {
        if (!HasCsrfHeader()) return MissingCsrfHeader();
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var validator = new UpdateProfileRequestValidator();
        var validation = await validator.ValidateAsync(request, CancellationToken.None);
        if (!validation.IsValid)
            return BadRequest(Error("Validation.Failed", validation.Errors.First().ErrorMessage));

        var result = await _profile.UpdateProfileAsync(userId, request, ct);
        if (!result.IsSuccess) return NotFound(Error(result.Error!.Code, result.Error.Message));
        return Ok(result.Value);
    }

    [HttpPut("me/username")]
    [Authorize]
    [SwaggerOperation(Summary = "Change the authenticated user's username/handle",
        OperationId = "Profile_UpdateUsername", Tags = new[] { "Profile" })]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateUsername([FromBody] UpdateUsernameRequestDto request, CancellationToken ct)
    {
        if (!HasCsrfHeader()) return MissingCsrfHeader();
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var validator = new UpdateUsernameRequestValidator();
        var validation = await validator.ValidateAsync(request, CancellationToken.None);
        if (!validation.IsValid)
            return BadRequest(Error("Validation.Failed", validation.Errors.First().ErrorMessage));

        var result = await _profile.UpdateUsernameAsync(userId, request.Username, ct);
        if (!result.IsSuccess)
        {
            if (result.Error!.Code == "Profile.UsernameTaken") return Conflict(Error(result.Error.Code, result.Error.Message));
            return BadRequest(Error(result.Error.Code, result.Error.Message));
        }
        return NoContent();
    }

    // ── Public profile ────────────────────────────────────────────────────────

    [HttpGet("{username}")]
    [SwaggerOperation(Summary = "Get a public profile by username",
        OperationId = "Profile_GetPublic", Tags = new[] { "Profile" })]
    [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublicProfile([FromRoute] string username, CancellationToken ct)
    {
        Guid? requesterId = null;
        if (TryGetUserId(out var id)) requesterId = id;

        var result = await _profile.GetPublicProfileAsync(username, requesterId, ct);
        if (!result.IsSuccess)
        {
            return result.Error!.Code switch
            {
                "General.NotFound" => NotFound(Error(result.Error.Code, result.Error.Message)),
                _ => StatusCode(StatusCodes.Status403Forbidden, Error(result.Error.Code, result.Error.Message))
            };
        }
        return Ok(result.Value);
    }

    // ── Links ────────────────────────────────────────────────────────────────

    [HttpGet("me/links")]
    [Authorize]
    [SwaggerOperation(Summary = "Get the authenticated user's external links",
        OperationId = "Profile_GetLinks", Tags = new[] { "Profile" })]
    [ProducesResponseType(typeof(IReadOnlyList<ExternalLinkDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetLinks(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _profile.GetLinksAsync(userId, ct);
        return Ok(result.Value);
    }

    [HttpPut("me/links")]
    [Authorize]
    [SwaggerOperation(Summary = "Replace the authenticated user's external links",
        OperationId = "Profile_UpdateLinks", Tags = new[] { "Profile" })]
    [ProducesResponseType(typeof(IReadOnlyList<ExternalLinkDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateLinks([FromBody] UpdateLinksRequestDto request, CancellationToken ct)
    {
        if (!HasCsrfHeader()) return MissingCsrfHeader();
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var validator = new UpdateLinksRequestValidator();
        var validation = await validator.ValidateAsync(request, CancellationToken.None);
        if (!validation.IsValid)
            return BadRequest(Error("Validation.Failed", validation.Errors.First().ErrorMessage));

        var result = await _profile.UpdateLinksAsync(userId, request, ct);
        return Ok(result.Value);
    }

    // ── Interests ─────────────────────────────────────────────────────────────

    [HttpGet("me/interests")]
    [Authorize]
    [SwaggerOperation(Summary = "Get the authenticated user's game interest tags",
        OperationId = "Profile_GetInterests", Tags = new[] { "Profile" })]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetInterests(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _profile.GetInterestsAsync(userId, ct);
        return Ok(result.Value);
    }

    [HttpPut("me/interests")]
    [Authorize]
    [SwaggerOperation(Summary = "Replace the authenticated user's game interest tags",
        OperationId = "Profile_UpdateInterests", Tags = new[] { "Profile" })]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateInterests([FromBody] UpdateInterestsRequestDto request, CancellationToken ct)
    {
        if (!HasCsrfHeader()) return MissingCsrfHeader();
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var validator = new UpdateInterestsRequestValidator();
        var validation = await validator.ValidateAsync(request, CancellationToken.None);
        if (!validation.IsValid)
            return BadRequest(Error("Validation.Failed", validation.Errors.First().ErrorMessage));

        var result = await _profile.UpdateInterestsAsync(userId, request, ct);
        return Ok(result.Value);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private const string CsrfHeader = "X-Requested-With";
    private const string CsrfHeaderValue = "XMLHttpRequest";

    private bool HasCsrfHeader() =>
        string.Equals(Request.Headers[CsrfHeader], CsrfHeaderValue, StringComparison.Ordinal);

    private IActionResult MissingCsrfHeader() => BadRequest(Error(
        "Auth.CsrfHeaderRequired",
        $"The {CsrfHeader} header is required for this request."));

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out userId);

    private static ApiErrorResponse Error(string code, string message) =>
        new(new ApiErrorDetail(code, message));
}
