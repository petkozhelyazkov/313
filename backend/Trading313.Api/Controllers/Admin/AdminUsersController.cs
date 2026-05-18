using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trading313.Api.Dtos.Admin;
using Trading313.Api.Infrastructure.Seeding;
using Trading313.Api.Services.Admin;

namespace Trading313.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = RoleNames.Admin)]
[Produces("application/json")]
public class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _admin;

    public AdminUsersController(IAdminUserService admin)
    {
        _admin = admin;
    }

    [HttpGet]
    [ProducesResponseType(typeof(AdminUserListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? email = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _admin.ListAsync(GetUserId(), page, pageSize, email, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string id, CancellationToken cancellationToken)
    {
        var user = await _admin.GetAsync(id, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPut("{id}/role")]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetRole(string id, [FromBody] UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await _admin.SetRoleAsync(GetUserId(), id, request.Role, cancellationToken);
        return AsActionResult(result);
    }

    [HttpPut("{id}/active")]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetActive(string id, [FromBody] SetActiveRequest request, CancellationToken cancellationToken)
    {
        var result = await _admin.SetActiveAsync(GetUserId(), id, request.IsActive, cancellationToken);
        return AsActionResult(result);
    }

    private IActionResult AsActionResult(AdminOpResult result)
    {
        if (result.Succeeded) return Ok(result.Value);
        var status = result.FailureKind == AdminFailureKind.NotFound
            ? StatusCodes.Status404NotFound
            : StatusCodes.Status400BadRequest;
        return Problem(statusCode: status, title: result.FailureKind.ToString(), detail: result.ErrorMessage);
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Authenticated user missing NameIdentifier claim.");
}
