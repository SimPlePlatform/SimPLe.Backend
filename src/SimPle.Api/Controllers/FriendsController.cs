using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimPle.Api.Models;
using SimPle.Application.Friends.DTOs;
using SimPle.Application.Friends.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace SimPle.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/friends")]
[Produces("application/json")]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
public sealed class FriendsController : ControllerBase
{
    private readonly IFriendService _friends;

    public FriendsController(IFriendService friends) => _friends = friends;

    [HttpGet]
    [SwaggerOperation(Summary = "Get the authenticated user's friends", OperationId = "Friends_List", Tags = new[] { "Friends" })]
    [ProducesResponseType(typeof(IReadOnlyList<UserSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFriends(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _friends.GetFriendsAsync(userId, ct);
        return Ok(result.Value);
    }

    [HttpGet("requests")]
    [SwaggerOperation(Summary = "Get incoming and outgoing friend requests", OperationId = "Friends_Requests", Tags = new[] { "Friends" })]
    [ProducesResponseType(typeof(FriendRequestsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRequests(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _friends.GetRequestsAsync(userId, ct);
        return Ok(result.Value);
    }

    [HttpGet("requests/incoming")]
    public async Task<IActionResult> GetIncoming(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _friends.GetIncomingRequestsAsync(userId, ct);
        return Ok(result.Value);
    }

    [HttpGet("requests/outgoing")]
    public async Task<IActionResult> GetOutgoing(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _friends.GetOutgoingRequestsAsync(userId, ct);
        return Ok(result.Value);
    }

    [HttpPost("requests")]
    [ProducesResponseType(typeof(FriendRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SendRequest([FromBody] SendFriendRequestDto request, CancellationToken ct)
    {
        if (!HasCsrfHeader()) return MissingCsrfHeader();
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await _friends.SendRequestAsync(userId, request.UserId, ct);
        return ToActionResult(result);
    }

    [HttpPost("requests/{requestId:guid}/accept")]
    public async Task<IActionResult> Accept(Guid requestId, CancellationToken ct)
    {
        if (!HasCsrfHeader()) return MissingCsrfHeader();
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return ToActionResult(await _friends.AcceptRequestAsync(userId, requestId, ct));
    }

    [HttpPost("requests/{requestId:guid}/decline")]
    public async Task<IActionResult> Decline(Guid requestId, CancellationToken ct)
    {
        if (!HasCsrfHeader()) return MissingCsrfHeader();
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return ToActionResult(await _friends.DeclineRequestAsync(userId, requestId, ct));
    }

    [HttpPost("requests/{requestId:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid requestId, CancellationToken ct)
    {
        if (!HasCsrfHeader()) return MissingCsrfHeader();
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return ToActionResult(await _friends.CancelRequestAsync(userId, requestId, ct));
    }

    [HttpDelete("{friendUserId:guid}")]
    public async Task<IActionResult> RemoveFriend(Guid friendUserId, CancellationToken ct)
    {
        if (!HasCsrfHeader()) return MissingCsrfHeader();
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _friends.RemoveFriendAsync(userId, friendUserId, ct);
        if (!result.IsSuccess) return ToErrorResult(result.Error!);
        return NoContent();
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(IReadOnlyList<UserSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] string query, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _friends.SearchAsync(userId, query ?? string.Empty, ct);
        if (!result.IsSuccess) return BadRequest(Error(result.Error!.Code, result.Error.Message));
        return Ok(result.Value);
    }

    [HttpGet("suggestions")]
    [ProducesResponseType(typeof(IReadOnlyList<UserSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Suggestions(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _friends.SuggestionsAsync(userId, ct);
        return Ok(result.Value);
    }

    [HttpGet("~/api/blocks")]
    [ProducesResponseType(typeof(IReadOnlyList<BlockedUserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBlocks(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _friends.GetBlocksAsync(userId, ct);
        return Ok(result.Value);
    }

    [HttpPost("~/api/blocks")]
    [ProducesResponseType(typeof(BlockedUserDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Block([FromBody] BlockUserRequestDto request, CancellationToken ct)
    {
        if (!HasCsrfHeader()) return MissingCsrfHeader();
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return ToActionResult(await _friends.BlockAsync(userId, request.UserId, ct));
    }

    [HttpDelete("~/api/blocks/{blockedUserId:guid}")]
    public async Task<IActionResult> Unblock(Guid blockedUserId, CancellationToken ct)
    {
        if (!HasCsrfHeader()) return MissingCsrfHeader();
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _friends.UnblockAsync(userId, blockedUserId, ct);
        if (!result.IsSuccess) return ToErrorResult(result.Error!);
        return NoContent();
    }

    [HttpGet("privacy")]
    [ProducesResponseType(typeof(FriendPrivacyDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPrivacy(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _friends.GetPrivacyAsync(userId, ct);
        return Ok(result.Value);
    }

    [HttpPut("privacy")]
    [ProducesResponseType(typeof(FriendPrivacyDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePrivacy([FromBody] UpdateFriendPrivacyDto request, CancellationToken ct)
    {
        if (!HasCsrfHeader()) return MissingCsrfHeader();
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return ToActionResult(await _friends.UpdatePrivacyAsync(userId, request.FriendRequestPolicy, ct));
    }

    private IActionResult ToActionResult<T>(SimPle.Shared.Common.Result<T> result)
    {
        if (result.IsSuccess) return Ok(result.Value);
        return ToErrorResult(result.Error!);
    }

    private IActionResult ToErrorResult(SimPle.Shared.Common.Error error) =>
        error.Code switch
        {
            "General.NotFound" or "Friends.RequestNotFound" or "Friends.NotFriends" => NotFound(Error(error.Code, error.Message)),
            "Friends.AlreadyFriends" or "Friends.PendingRequestExists" => Conflict(Error(error.Code, error.Message)),
            "Friends.Forbidden" => StatusCode(StatusCodes.Status403Forbidden, Error(error.Code, error.Message)),
            _ => BadRequest(Error(error.Code, error.Message))
        };

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
