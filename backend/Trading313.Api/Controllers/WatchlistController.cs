using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trading313.Api.Dtos.Watchlist;
using Trading313.Api.Services.Watchlist;

namespace Trading313.Api.Controllers;

/// <summary>
/// Watchlist CRUD with support for multiple named lists.
/// </summary>
[ApiController]
[Route("api/watchlist")]
[Authorize]
[Produces("application/json")]
public class WatchlistController : ControllerBase
{
    private readonly IWatchlistService _watchlist;

    public WatchlistController(IWatchlistService watchlist)
    {
        _watchlist = watchlist;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<WatchlistItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromQuery] string? list, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var items = await _watchlist.GetAllAsync(userId, list, cancellationToken);
        return Ok(items);
    }

    [HttpGet("lists")]
    [ProducesResponseType(typeof(IEnumerable<WatchlistSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLists(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var lists = await _watchlist.GetListsAsync(userId, cancellationToken);
        return Ok(lists);
    }

    [HttpPost]
    [ProducesResponseType(typeof(WatchlistItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Add([FromBody] AddToWatchlistRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var result = await _watchlist.AddAsync(userId, request.Symbol, request.Notes, request.ListName, cancellationToken);
        if (result.Succeeded)
            return StatusCode(StatusCodes.Status201Created, result.Value);

        var status = result.FailureKind switch
        {
            WatchlistFailureKind.AlreadyExists => StatusCodes.Status409Conflict,
            WatchlistFailureKind.SymbolNotResolved => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status400BadRequest,
        };
        return Problem(statusCode: status, title: result.FailureKind.ToString(), detail: result.ErrorMessage);
    }

    [HttpDelete("{symbol}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(string symbol, [FromQuery] string? list, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var result = await _watchlist.RemoveAsync(userId, symbol, list, cancellationToken);
        return result.Succeeded
            ? NoContent()
            : Problem(statusCode: StatusCodes.Status404NotFound, title: result.FailureKind.ToString(), detail: result.ErrorMessage);
    }

    [HttpPatch("{symbol}")]
    [ProducesResponseType(typeof(WatchlistItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateNotes(string symbol, [FromBody] UpdateWatchlistNotesRequest request, [FromQuery] string? list, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var result = await _watchlist.UpdateNotesAsync(userId, symbol, request.Notes, list, cancellationToken);
        return result.Succeeded
            ? Ok(result.Value)
            : Problem(statusCode: StatusCodes.Status404NotFound, title: result.FailureKind.ToString(), detail: result.ErrorMessage);
    }

    [HttpPut("lists/{name}/rename")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RenameList(string name, [FromBody] RenameWatchlistRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var result = await _watchlist.RenameListAsync(userId, name, request.NewName, cancellationToken);
        if (result.Succeeded) return NoContent();
        var status = result.FailureKind == WatchlistFailureKind.AlreadyExists
            ? StatusCodes.Status409Conflict
            : StatusCodes.Status400BadRequest;
        return Problem(statusCode: status, title: result.FailureKind.ToString(), detail: result.ErrorMessage);
    }

    [HttpDelete("lists/{name}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteList(string name, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var result = await _watchlist.DeleteListAsync(userId, name, cancellationToken);
        if (result.Succeeded) return NoContent();
        var status = result.FailureKind == WatchlistFailureKind.NotFound
            ? StatusCodes.Status404NotFound
            : StatusCodes.Status400BadRequest;
        return Problem(statusCode: status, title: result.FailureKind.ToString(), detail: result.ErrorMessage);
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Authenticated user missing NameIdentifier claim.");
}
