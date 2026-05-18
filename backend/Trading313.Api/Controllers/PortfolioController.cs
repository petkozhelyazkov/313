using System.Globalization;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Trading313.Api.Data;
using Trading313.Api.Domain.Entities;
using Trading313.Api.Dtos.Portfolio;
using Trading313.Api.Services.Portfolio;

namespace Trading313.Api.Controllers;

/// <summary>
/// Authenticated portfolio operations: summary, transactions, buy, sell, CSV exports.
/// </summary>
[ApiController]
[Route("api/portfolio")]
[Authorize]
[Produces("application/json")]
public class PortfolioController : ControllerBase
{
    private readonly IPortfolioService _portfolio;
    private readonly IPortfolioQueryService _query;
    private readonly ITaxReportService _tax;
    private readonly AppDbContext _db;

    public PortfolioController(IPortfolioService portfolio, IPortfolioQueryService query, ITaxReportService tax, AppDbContext db)
    {
        _portfolio = portfolio;
        _query = query;
        _tax = tax;
        _db = db;
    }

    /// <summary>List tax years for which the user has activity.</summary>
    [HttpGet("tax-report/years")]
    [ProducesResponseType(typeof(IEnumerable<int>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTaxYears(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var years = await _tax.AvailableYearsAsync(userId, cancellationToken);
        return Ok(years);
    }

    /// <summary>Full tax report for a year — short/long-term gains, dividends, fees.</summary>
    [HttpGet("tax-report/{year:int}")]
    [ProducesResponseType(typeof(TaxReportResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTaxReport(int year, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var report = await _tax.GenerateAsync(userId, year, cancellationToken);
        return Ok(report);
    }

    /// <summary>Tax report as CSV (one row per matched sell + dividend lines + summary).</summary>
    [HttpGet("tax-report/{year:int}.csv")]
    [Produces("text/csv")]
    public async Task<IActionResult> GetTaxReportCsv(int year, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var report = await _tax.GenerateAsync(userId, year, cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine($"Tax Report {report.Year}");
        sb.AppendLine();
        sb.AppendLine("# Summary");
        sb.AppendLine($"Short-term gains,{report.ShortTermGains.ToString(CultureInfo.InvariantCulture)}");
        sb.AppendLine($"Short-term losses,{report.ShortTermLosses.ToString(CultureInfo.InvariantCulture)}");
        sb.AppendLine($"Short-term net,{report.ShortTermNet.ToString(CultureInfo.InvariantCulture)}");
        sb.AppendLine($"Long-term gains,{report.LongTermGains.ToString(CultureInfo.InvariantCulture)}");
        sb.AppendLine($"Long-term losses,{report.LongTermLosses.ToString(CultureInfo.InvariantCulture)}");
        sb.AppendLine($"Long-term net,{report.LongTermNet.ToString(CultureInfo.InvariantCulture)}");
        sb.AppendLine($"Dividends received,{report.DividendsReceived.ToString(CultureInfo.InvariantCulture)}");
        sb.AppendLine($"Fees paid,{report.FeesPaid.ToString(CultureInfo.InvariantCulture)}");
        sb.AppendLine($"Net total,{report.NetTotal.ToString(CultureInfo.InvariantCulture)}");
        sb.AppendLine();
        sb.AppendLine("# Realized sells");
        sb.AppendLine("Symbol,Acquired,Sold,Quantity,CostBasis,Proceeds,Gain,Term");
        foreach (var r in report.SellRows)
        {
            sb.Append(Csv(r.Symbol)).Append(',');
            sb.Append(r.AcquiredAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append(',');
            sb.Append(r.SoldAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append(',');
            sb.Append(r.Quantity.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append(r.CostBasis.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append(r.Proceeds.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append(r.Gain.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.AppendLine(r.IsLongTerm ? "Long" : "Short");
        }
        sb.AppendLine();
        sb.AppendLine("# Dividends");
        sb.AppendLine("Symbol,ExDate,PerShare,Quantity,Total");
        foreach (var d in report.DividendRows)
        {
            sb.Append(Csv(d.Symbol)).Append(',');
            sb.Append(d.ExDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append(',');
            sb.Append(d.AmountPerShare.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append(d.QuantityAtExDate.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.AppendLine(d.TotalReceived.ToString(CultureInfo.InvariantCulture));
        }
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"tax-report-{year}.csv");
    }

    /// <summary>Aggregate portfolio summary: cash, holdings value, P/L, per-symbol positions.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PortfolioSummary), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary([FromQuery] bool includeClosed = false, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        var summary = await _query.GetSummaryAsync(userId, includeClosed, cancellationToken);
        return Ok(summary);
    }

    /// <summary>Update notes and tags on the user's position for a symbol.</summary>
    [HttpPut("positions/{symbol}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePosition(string symbol, [FromBody] UpdatePositionRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var sym = symbol.Trim().ToUpperInvariant();
        var position = await _db.Positions
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Symbol == sym, cancellationToken);
        if (position is null) return NotFound();
        if (request.Notes is not null) position.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        if (request.Tags is not null) position.Tags = string.IsNullOrWhiteSpace(request.Tags) ? null : request.Tags.Trim();
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    /// <summary>Paginated transaction history.</summary>
    [HttpGet("transactions")]
    [ProducesResponseType(typeof(TransactionListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? symbol = null,
        [FromQuery] string? tag = null,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        var result = await _query.GetTransactionsAsync(userId, page, pageSize, symbol, tag, cancellationToken);
        return Ok(result);
    }

    /// <summary>Update notes and tags on a specific transaction.</summary>
    [HttpPut("transactions/{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTransaction(long id, [FromBody] UpdateTransactionRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var txn = await _db.Transactions.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, cancellationToken);
        if (txn is null) return NotFound();
        if (request.Notes is not null) txn.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        if (request.Tags is not null) txn.Tags = string.IsNullOrWhiteSpace(request.Tags) ? null : request.Tags.Trim();
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    /// <summary>Tag-grouped P/L: aggregates realized P/L by tag for all sell transactions.</summary>
    [HttpGet("transactions/tag-summary")]
    [ProducesResponseType(typeof(IEnumerable<TagPlSummary>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTagSummary(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var rows = await _db.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.Tags != null)
            .Select(t => new { t.Tags, t.RealizedPl, t.Id })
            .ToListAsync(cancellationToken);

        var byTag = new Dictionary<string, (decimal Realized, int Count)>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in rows)
        {
            if (r.Tags is null) continue;
            foreach (var rawTag in r.Tags.Split(','))
            {
                var tag = rawTag.Trim();
                if (tag.Length == 0) continue;
                var prev = byTag.GetValueOrDefault(tag);
                byTag[tag] = (prev.Realized + (r.RealizedPl ?? 0m), prev.Count + 1);
            }
        }

        var summary = byTag
            .OrderByDescending(kv => kv.Value.Realized)
            .Select(kv => new TagPlSummary(kv.Key, kv.Value.Realized, kv.Value.Count))
            .ToList();
        return Ok(summary);
    }

    /// <summary>Transactions CSV download (no paging — exports everything for the current user).</summary>
    [HttpGet("transactions.csv")]
    [Produces("text/csv")]
    public async Task<IActionResult> ExportTransactionsCsv(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var rows = await _db.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.ExecutedAt)
            .Select(t => new
            {
                t.Id, t.Symbol, Type = t.Type.ToString(), t.Quantity, t.PricePerShare,
                t.Fees, t.TotalAmount, t.ExecutedAt, t.RealizedPl, t.Notes,
            })
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("Id,Symbol,Type,Quantity,PricePerShare,Fees,TotalAmount,ExecutedAt,RealizedPl,Notes");
        foreach (var r in rows)
        {
            sb.Append(r.Id).Append(',');
            sb.Append(Csv(r.Symbol)).Append(',');
            sb.Append(r.Type).Append(',');
            sb.Append(r.Quantity.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append(r.PricePerShare.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append(r.Fees.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append(r.TotalAmount.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append(r.ExecutedAt.ToString("o", CultureInfo.InvariantCulture)).Append(',');
            sb.Append(r.RealizedPl?.ToString(CultureInfo.InvariantCulture) ?? string.Empty).Append(',');
            sb.AppendLine(Csv(r.Notes));
        }
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"transactions-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    /// <summary>Positions CSV download (open + closed).</summary>
    [HttpGet("positions.csv")]
    [Produces("text/csv")]
    public async Task<IActionResult> ExportPositionsCsv(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var rows = await _db.Positions
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.Symbol)
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("Symbol,Quantity,AverageCost,TotalInvested,RealizedPlLifetime,FirstPurchasedAt,LastTransactionAt,IsClosed");
        foreach (var p in rows)
        {
            sb.Append(Csv(p.Symbol)).Append(',');
            sb.Append(p.Quantity.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append(p.AverageCost.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append(p.TotalInvested.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append(p.RealizedPlLifetime.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append(p.FirstPurchasedAt.ToString("o", CultureInfo.InvariantCulture)).Append(',');
            sb.Append(p.LastTransactionAt.ToString("o", CultureInfo.InvariantCulture)).Append(',');
            sb.AppendLine(p.IsClosed ? "true" : "false");
        }
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"positions-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    /// <summary>Deposit or withdraw virtual cash.</summary>
    [HttpPost("cash")]
    [ProducesResponseType(typeof(CashAdjustmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AdjustCash([FromBody] CashAdjustmentRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (request.Amount <= 0)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "InvalidAmount", detail: "Amount must be greater than 0.");

        await using var dbTx = await _db.Database.BeginTransactionAsync(cancellationToken);
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "UserNotFound", detail: "User not found.");

        if (request.Type == CashTransactionType.Withdraw && user.CashBalance < request.Amount)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "InsufficientFunds",
                detail: $"Cannot withdraw {request.Amount:F2}; current balance is {user.CashBalance:F2}.");
        }

        if (request.Type == CashTransactionType.Deposit) user.CashBalance += request.Amount;
        else user.CashBalance -= request.Amount;

        var entry = new CashTransaction
        {
            UserId = userId,
            Type = request.Type,
            Amount = request.Amount,
            BalanceAfter = user.CashBalance,
            ExecutedAt = DateTime.UtcNow,
            Notes = request.Notes,
        };
        _db.CashTransactions.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);
        await dbTx.CommitAsync(cancellationToken);

        return Ok(new CashAdjustmentResponse(
            new CashTransactionDto(entry.Id, entry.Type.ToString(), entry.Amount, entry.BalanceAfter, entry.ExecutedAt, entry.Notes),
            user.CashBalance));
    }

    /// <summary>List the user's cash deposits/withdrawals.</summary>
    [HttpGet("cash")]
    [ProducesResponseType(typeof(IEnumerable<CashTransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListCash(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var rows = await _db.CashTransactions
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.ExecutedAt)
            .Take(100)
            .Select(c => new CashTransactionDto(c.Id, c.Type.ToString(), c.Amount, c.BalanceAfter, c.ExecutedAt, c.Notes))
            .ToListAsync(cancellationToken);
        return Ok(rows);
    }

    /// <summary>Buy shares. Server fetches the current price — do not pass price from client.</summary>
    [HttpPost("buy")]
    [ProducesResponseType(typeof(TradeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Buy([FromBody] BuyRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var result = await _portfolio.BuyAsync(userId, request, cancellationToken);
        return result.Succeeded
            ? Ok(result.Value)
            : Problem(statusCode: StatusCodes.Status400BadRequest,
                      title: result.FailureKind.ToString(),
                      detail: result.ErrorMessage);
    }

    /// <summary>Sell shares. Server fetches the current price.</summary>
    [HttpPost("sell")]
    [ProducesResponseType(typeof(TradeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Sell([FromBody] SellRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var result = await _portfolio.SellAsync(userId, request, cancellationToken);
        return result.Succeeded
            ? Ok(result.Value)
            : Problem(statusCode: StatusCodes.Status400BadRequest,
                      title: result.FailureKind.ToString(),
                      detail: result.ErrorMessage);
    }

    private static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Authenticated user missing NameIdentifier claim.");
}
