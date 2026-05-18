using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Trading313.Api.Data;
using Trading313.Api.Domain.Entities;
using Trading313.Api.Dtos.Users;

namespace Trading313.Api.Services.Users;

public interface IAchievementService
{
    Task<IReadOnlyList<AchievementDto>> GetAchievementsAsync(string userId, CancellationToken cancellationToken);
}

public class AchievementService : IAchievementService
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public AchievementService(AppDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public async Task<IReadOnlyList<AchievementDto>> GetAchievementsAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await _users.FindByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        var transactions = await _db.Transactions
            .Where(t => t.UserId == userId)
            .Select(t => new { t.Symbol, t.Type, t.Quantity, t.PricePerShare, t.ExecutedAt, t.RealizedPl })
            .ToListAsync(cancellationToken);

        var positions = await _db.Positions
            .Where(p => p.UserId == userId && !p.IsClosed && p.Quantity > 0)
            .Select(p => new { p.Symbol, p.Quantity, p.AverageCost, p.FirstPurchasedAt })
            .ToListAsync(cancellationToken);

        var watchlistCount = await _db.WatchlistItems.CountAsync(w => w.UserId == userId, cancellationToken);

        var totalInvested = transactions
            .Where(t => t.Type == Domain.Enums.TransactionType.Buy)
            .Sum(t => t.Quantity * t.PricePerShare);

        var totalRealizedPl = transactions.Sum(t => t.RealizedPl ?? 0m);

        var symbols = positions.Select(p => p.Symbol).ToList();
        var sectorCount = 0;
        if (symbols.Count > 0)
        {
            sectorCount = await _db.Stocks
                .Where(s => symbols.Contains(s.Symbol) && s.Sector != null && s.Sector != "")
                .Select(s => s.Sector)
                .Distinct()
                .CountAsync(cancellationToken);
        }

        decimal portfolioValue = positions.Sum(p => p.Quantity * p.AverageCost);
        decimal largestPositionPct = 0;
        if (portfolioValue > 0)
        {
            largestPositionPct = positions.Max(p => (p.Quantity * p.AverageCost) / portfolioValue) * 100m;
        }

        var longestHoldDays = 0;
        if (positions.Count > 0)
        {
            longestHoldDays = positions.Max(p => (int)(DateTime.UtcNow - p.FirstPurchasedAt).TotalDays);
        }

        DateTime? firstTradeAt = transactions.Count > 0 ? transactions.Min(t => t.ExecutedAt) : null;

        var list = new List<AchievementDto>
        {
            Make("first-steps", "First Steps", "Welcome aboard — your account is ready.", "sr-sunrise",
                earned: true, earnedAt: user.CreatedAt),

            Make("first-trade", "First Trade", "Place your first transaction.", "sr-medal",
                earned: transactions.Count > 0, earnedAt: firstTradeAt,
                progress: Math.Min(transactions.Count, 1), target: 1),

            Make("portfolio-builder", "Portfolio Builder", "Hold 5 different stocks at the same time.", "sr-briefcase",
                earned: positions.Count >= 5, earnedAt: null,
                progress: Math.Min(positions.Count, 5), target: 5),

            Make("sector-spread", "Sector Spread", "Hold stocks across 3 or more sectors.", "sr-globe",
                earned: sectorCount >= 3, earnedAt: null,
                progress: Math.Min(sectorCount, 3), target: 3),

            Make("big-spender", "Big Spender", "Invest $5,000 or more in total.", "sr-sack-dollar",
                earned: totalInvested >= 5_000m, earnedAt: null,
                progress: (int)Math.Min(totalInvested, 5_000m), target: 5_000),

            Make("watcher", "Watcher", "Add 5 stocks to your watchlist.", "sr-eye",
                earned: watchlistCount >= 5, earnedAt: null,
                progress: Math.Min(watchlistCount, 5), target: 5),

            Make("well-balanced", "Well-Balanced", "Keep no single position above 25% of your portfolio (5+ positions).", "sr-balance-scale-left",
                earned: positions.Count >= 5 && largestPositionPct < 25m && largestPositionPct > 0,
                earnedAt: null,
                progress: null, target: null),

            Make("centurion", "Centurion", "Make 100 transactions.", "sr-bullseye",
                earned: transactions.Count >= 100, earnedAt: null,
                progress: Math.Min(transactions.Count, 100), target: 100),

            Make("patient-investor", "Patient Investor", "Hold a position for 30 days or more.", "sr-books",
                earned: longestHoldDays >= 30, earnedAt: null,
                progress: Math.Min(longestHoldDays, 30), target: 30),

            Make("in-the-green", "In The Green", "Realize positive lifetime profit.", "sr-chart-line-up",
                earned: totalRealizedPl > 0, earnedAt: null,
                progress: null, target: null),
        };

        return list;
    }

    private static AchievementDto Make(
        string code, string name, string desc, string icon,
        bool earned, DateTime? earnedAt = null,
        int? progress = null, int? target = null)
        => new(code, name, desc, icon, earned, earned ? earnedAt : null, progress, target);
}
