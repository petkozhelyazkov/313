using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trading313.Api.Dtos.Orders;
using Trading313.Api.Services.Orders;

namespace Trading313.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IOrdersService _orders;

    public OrdersController(IOrdersService orders)
    {
        _orders = orders;
    }

    [HttpGet]
    [ProducesResponseType(typeof(OrderListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _orders.ListAsync(GetUserId(), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PendingOrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Place([FromBody] PlaceOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await _orders.PlaceAsync(GetUserId(), request, cancellationToken);
        return result.Succeeded
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : Problem(statusCode: StatusCodes.Status400BadRequest, title: result.FailureKind.ToString(), detail: result.ErrorMessage);
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(typeof(PendingOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(long id, CancellationToken cancellationToken)
    {
        var result = await _orders.CancelAsync(GetUserId(), id, cancellationToken);
        if (result.Succeeded) return Ok(result.Value);
        var status = result.FailureKind == OrderFailureKind.NotFound
            ? StatusCodes.Status404NotFound
            : StatusCodes.Status400BadRequest;
        return Problem(statusCode: status, title: result.FailureKind.ToString(), detail: result.ErrorMessage);
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Authenticated user missing NameIdentifier claim.");
}
