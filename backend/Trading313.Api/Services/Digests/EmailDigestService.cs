using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Trading313.Api.Data;
using Trading313.Api.Domain.Entities;
using Trading313.Api.Domain.Enums;
using Trading313.Api.Services.MarketData;

namespace Trading313.Api.Services.Digests;

public class SmtpOptions
{
    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string FromAddress { get; set; } = "noreply@trading313.local";
    public string FromName { get; set; } = "Trading313";
    public bool EnableSsl { get; set; } = true;
}

public interface IEmailDigestService
{
    Task<EmailDigest?> GenerateForUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<int> RunWeeklyForAllUsersAsync(CancellationToken cancellationToken = default);
}

public class EmailDigestService : IEmailDigestService
{
    private readonly AppDbContext _db;
    private readonly IQuoteService _quotes;
    private readonly SmtpOptions _smtp;
    private readonly ILogger<EmailDigestService> _logger;

    public EmailDigestService(
        AppDbContext db,
        IQuoteService quotes,
        IOptions<SmtpOptions> smtp,
        ILogger<EmailDigestService> logger)
    {
        _db = db;
        _quotes = quotes;
        _smtp = smtp.Value;
        _logger = logger;
    }

    public async Task<EmailDigest?> GenerateForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Set<ApplicationUser>().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null) return null;
        if (!user.EmailDigestEnabled)
        {
            _logger.LogDebug("Skipping digest for {UserId} — opted out", userId);
            return null;
        }

        var now = DateTime.UtcNow;
        var periodStart = now.AddDays(-7);

        var txns = await _db.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.ExecutedAt >= periodStart)
            .OrderBy(t => t.ExecutedAt)
            .ToListAsync(cancellationToken);

        var positions = await _db.Positions
            .AsNoTracking()
            .Where(p => p.UserId == userId && !p.IsClosed && p.Quantity > 0)
            .ToListAsync(cancellationToken);

        decimal holdingsValue = 0m, costBasis = 0m;
        foreach (var p in positions)
        {
            var quote = await _quotes.GetQuoteAsync(p.Symbol, cancellationToken);
            var price = quote?.Price ?? p.AverageCost;
            holdingsValue += p.Quantity * price;
            costBasis += p.Quantity * p.AverageCost;
        }
        var totalValue = user.CashBalance + holdingsValue;
        var unrealized = holdingsValue - costBasis;

        var buys = txns.Count(t => t.Type == TransactionType.Buy);
        var sells = txns.Count(t => t.Type == TransactionType.Sell);
        var realizedThisWeek = txns.Where(t => t.RealizedPl.HasValue).Sum(t => t.RealizedPl!.Value);

        var subject = $"Trading313 Weekly Digest · {now:MMM d, yyyy}";
        var html = BuildHtml(user, totalValue, holdingsValue, unrealized, buys, sells, realizedThisWeek, txns, positions);
        var text = BuildText(user, totalValue, holdingsValue, unrealized, buys, sells, realizedThisWeek, txns, positions);

        var digest = new EmailDigest
        {
            UserId = userId,
            PeriodStart = periodStart,
            PeriodEnd = now,
            Subject = subject,
            BodyHtml = html,
            BodyText = text,
            GeneratedAt = now,
        };
        _db.EmailDigests.Add(digest);
        await _db.SaveChangesAsync(cancellationToken);

        // Optional SMTP send. If SMTP isn't configured we just persist the digest
        // — the user can still read it from the in-app digests view.
        if (!string.IsNullOrWhiteSpace(_smtp.Host) && !string.IsNullOrWhiteSpace(user.Email))
        {
            try
            {
                using var msg = new MailMessage
                {
                    From = new MailAddress(_smtp.FromAddress, _smtp.FromName),
                    Subject = subject,
                    Body = html,
                    IsBodyHtml = true,
                };
                msg.To.Add(user.Email);
                using var client = new SmtpClient(_smtp.Host, _smtp.Port)
                {
                    EnableSsl = _smtp.EnableSsl,
                    Credentials = string.IsNullOrEmpty(_smtp.Username)
                        ? null
                        : new NetworkCredential(_smtp.Username, _smtp.Password),
                };
                await client.SendMailAsync(msg, cancellationToken);
                digest.SentAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SMTP send failed for {UserId}; digest persisted but not emailed", userId);
            }
        }

        return digest;
    }

    public async Task<int> RunWeeklyForAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var userIds = await _db.Set<ApplicationUser>()
            .Where(u => u.IsActive && u.EmailDigestEnabled)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        int generated = 0;
        foreach (var userId in userIds)
        {
            try
            {
                var d = await GenerateForUserAsync(userId, cancellationToken);
                if (d is not null) generated++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Digest generation failed for {UserId}", userId);
            }
        }
        return generated;
    }

    private static string BuildHtml(
        ApplicationUser user, decimal totalValue, decimal holdings, decimal unrealized,
        int buys, int sells, decimal realized,
        IList<Transaction> txns, IList<Position> positions)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<html><body style='font-family:sans-serif;color:#1f2937;'>");
        sb.AppendLine($"<h2>Hi {WebUtility.HtmlEncode(user.DisplayName)},</h2>");
        sb.AppendLine("<p>Here's your weekly snapshot.</p>");
        sb.AppendLine("<h3>Portfolio</h3>");
        sb.AppendLine("<ul>");
        sb.AppendLine($"<li>Total value: <strong>${totalValue:N2}</strong></li>");
        sb.AppendLine($"<li>Cash: <strong>${user.CashBalance:N2}</strong></li>");
        sb.AppendLine($"<li>Holdings: <strong>${holdings:N2}</strong></li>");
        sb.AppendLine($"<li>Unrealized P/L: <strong>${unrealized:N2}</strong></li>");
        sb.AppendLine($"<li>Open positions: <strong>{positions.Count}</strong></li>");
        sb.AppendLine("</ul>");
        sb.AppendLine("<h3>This week</h3>");
        sb.AppendLine("<ul>");
        sb.AppendLine($"<li>Buys: <strong>{buys}</strong></li>");
        sb.AppendLine($"<li>Sells: <strong>{sells}</strong></li>");
        sb.AppendLine($"<li>Realized P/L: <strong>${realized:N2}</strong></li>");
        sb.AppendLine("</ul>");
        if (txns.Count > 0)
        {
            sb.AppendLine("<h3>Trades</h3>");
            sb.AppendLine("<table cellpadding='6' cellspacing='0' border='1' style='border-collapse:collapse;border-color:#e5e7eb;'>");
            sb.AppendLine("<tr><th>Date</th><th>Symbol</th><th>Type</th><th>Qty</th><th>Price</th><th>Total</th></tr>");
            foreach (var t in txns.TakeLast(20))
            {
                sb.AppendLine($"<tr><td>{t.ExecutedAt:yyyy-MM-dd}</td><td>{t.Symbol}</td><td>{t.Type}</td><td>{t.Quantity}</td><td>${t.PricePerShare:N2}</td><td>${t.TotalAmount:N2}</td></tr>");
            }
            sb.AppendLine("</table>");
        }
        sb.AppendLine("<p style='color:#6b7280;font-size:12px;margin-top:24px;'>You're receiving this because weekly digests are enabled on your account. Manage in your profile.</p>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static string BuildText(
        ApplicationUser user, decimal totalValue, decimal holdings, decimal unrealized,
        int buys, int sells, decimal realized,
        IList<Transaction> txns, IList<Position> positions)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Hi {user.DisplayName},");
        sb.AppendLine();
        sb.AppendLine("Here's your weekly snapshot.");
        sb.AppendLine();
        sb.AppendLine("PORTFOLIO");
        sb.AppendLine($"  Total value:    ${totalValue:N2}");
        sb.AppendLine($"  Cash:           ${user.CashBalance:N2}");
        sb.AppendLine($"  Holdings:       ${holdings:N2}");
        sb.AppendLine($"  Unrealized P/L: ${unrealized:N2}");
        sb.AppendLine($"  Open positions: {positions.Count}");
        sb.AppendLine();
        sb.AppendLine("THIS WEEK");
        sb.AppendLine($"  Buys:           {buys}");
        sb.AppendLine($"  Sells:          {sells}");
        sb.AppendLine($"  Realized P/L:   ${realized:N2}");
        if (txns.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("TRADES");
            foreach (var t in txns.TakeLast(20))
            {
                sb.AppendLine($"  {t.ExecutedAt:yyyy-MM-dd}  {t.Symbol,-6} {t.Type,-4} {t.Quantity,8} @ ${t.PricePerShare:N2}");
            }
        }
        return sb.ToString();
    }
}
