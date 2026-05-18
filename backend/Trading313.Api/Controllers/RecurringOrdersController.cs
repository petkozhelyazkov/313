using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trading313.Api.Dtos.RecurringOrders;
using Trading313.Api.Services.RecurringOrders;

namespace Trading313.Api.Controllers;

[ApiController]
[Route("api/recurring-orders")]
[Authorize]
[Produces("application/json")]
public class RecurringOrdersController : ControllerBase
{
    private readonly IRecurringOrdersService _service;

    public RecurringOrdersController(IRecurringOrdersService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<RecurringOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
        => Ok(await _service.GetForUserAsync(GetUserId(), cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(RecurringOrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateRecurringOrderRequest request, CancellationToken cancellationToken)
    {
        var (ok, error, value) = await _service.CreateAsync(GetUserId(), request, cancellationToken);
        if (!ok) return Problem(statusCode: StatusCodes.Status400BadRequest, detail: error);
        return StatusCode(StatusCodes.Status201Created, value);
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateRecurringOrderRequest request, CancellationToken cancellationToken)
        => await _service.UpdateAsync(GetUserId(), id, request, cancellationToken) ? NoContent() : NotFound();

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        => await _service.DeleteAsync(GetUserId(), id, cancellationToken) ? NoContent() : NotFound();

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Authenticated user missing NameIdentifier claim.");
}
