using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trading313.Api.Dtos.Goals;
using Trading313.Api.Services.Goals;

namespace Trading313.Api.Controllers;

[ApiController]
[Route("api/goals")]
[Authorize]
[Produces("application/json")]
public class GoalsController : ControllerBase
{
    private readonly IGoalsService _goals;

    public GoalsController(IGoalsService goals)
    {
        _goals = goals;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<GoalDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
        => Ok(await _goals.GetForUserAsync(GetUserId(), cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(GoalDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateGoalRequest request, CancellationToken cancellationToken)
    {
        var (ok, error, value) = await _goals.CreateAsync(GetUserId(), request, cancellationToken);
        if (!ok) return Problem(statusCode: StatusCodes.Status400BadRequest, detail: error);
        return StatusCode(StatusCodes.Status201Created, value);
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateGoalRequest request, CancellationToken cancellationToken)
        => await _goals.UpdateAsync(GetUserId(), id, request, cancellationToken) ? NoContent() : NotFound();

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        => await _goals.DeleteAsync(GetUserId(), id, cancellationToken) ? NoContent() : NotFound();

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Authenticated user missing NameIdentifier claim.");
}
