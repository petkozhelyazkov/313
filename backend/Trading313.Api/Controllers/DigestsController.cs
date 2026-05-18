using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Trading313.Api.Data;
using Trading313.Api.Domain.Entities;
using Trading313.Api.Services.Digests;

namespace Trading313.Api.Controllers;

[ApiController]
[Route("api/digests")]
[Authorize]
[Produces("application/json")]
public class DigestsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IEmailDigestService _digestSvc;

    public DigestsController(AppDbContext db, IEmailDigestService digestSvc)
    {
        _db = db;
        _digestSvc = digestSvc;
    }

    /// <summary>List recent digests for the current user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<DigestSummary>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var rows = await _db.EmailDigests
            .AsNoTracking()
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.GeneratedAt)
            .Take(20)
            .Select(d => new DigestSummary(
                d.Id, d.Subject, d.PeriodStart, d.PeriodEnd, d.GeneratedAt, d.SentAt, d.ReadAt != null))
            .ToListAsync(cancellationToken);
        return Ok(rows);
    }

    /// <summary>Get a single digest by id, including the rendered HTML body.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(DigestDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(long id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var d = await _db.EmailDigests.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);
        if (d is null) return NotFound();
        if (d.ReadAt is null)
        {
            d.ReadAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
        return Ok(new DigestDetail(d.Id, d.Subject, d.PeriodStart, d.PeriodEnd, d.GeneratedAt, d.SentAt, d.ReadAt, d.BodyHtml, d.BodyText));
    }

    /// <summary>Generate a digest right now (manual trigger — useful for demo + preview).</summary>
    [HttpPost("run-now")]
    [ProducesResponseType(typeof(DigestSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RunNow(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var d = await _digestSvc.GenerateForUserAsync(userId, cancellationToken);
        if (d is null)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "DigestDisabled",
                detail: "Enable the weekly digest in your profile preferences first.");
        }
        return Ok(new DigestSummary(d.Id, d.Subject, d.PeriodStart, d.PeriodEnd, d.GeneratedAt, d.SentAt, d.ReadAt != null));
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Authenticated user missing NameIdentifier claim.");
}

public record DigestSummary(
    long Id,
    string Subject,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    DateTime GeneratedAt,
    DateTime? SentAt,
    bool Read);

public record DigestDetail(
    long Id,
    string Subject,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    DateTime GeneratedAt,
    DateTime? SentAt,
    DateTime? ReadAt,
    string BodyHtml,
    string BodyText);
