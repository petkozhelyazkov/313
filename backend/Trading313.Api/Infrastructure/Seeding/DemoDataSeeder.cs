using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Trading313.Api.Data;
using Trading313.Api.Domain.Entities;
using Trading313.Api.Domain.Enums;
using Trading313.Api.Services.Analytics;
using Trading313.Api.Services.MarketData;

namespace Trading313.Api.Infrastructure.Seeding;

/// <summary>
/// Seeds three demo users (demo1/2/3@trading212.local, password Demo1234) with
/// ~15 backdated transactions each across 8 popular symbols, then triggers the
/// snapshot backfill so the Analytics page shows interesting curves.
/// </summary>
public class DemoDataSeeder
{
    private static readonly string[] DemoEmails =
    {
        "demo1@trading212.local",
        "demo2@trading212.local",
        "demo3@trading212.local",
    };

    private static readonly string[] DemoSymbols = { "AAPL", "MSFT", "GOOGL", "AMZN", "TSLA", "NVDA", "META", "AMD" };

    private readonly IServiceProvider _services;
    private readonly ILogger<DemoDataSeeder> _logger;

    public DemoDataSeeder(IServiceProvider services, ILogger<DemoDataSeeder> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _services.CreateScope();
        var sp = scope.ServiceProvider;
        var seedOptions = sp.GetRequiredService<IOptions<SeedOptions>>().Value;
        if (!seedOptions.Enabled)
        {
            _logger.LogInformation("Seed:Enabled is false — skipping demo data seeding.");
            return;
        }

        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var db = sp.GetRequiredService<AppDbContext>();
        var snapshots = sp.GetRequiredService<ISnapshotService>();
        var history = sp.GetRequiredService<IHistoryService>();

        // Pre-warm 5Y of historical prices for every demo symbol so generated
        // transactions and snapshot backfill use real historical closes.
        foreach (var symbol in DemoSymbols)
        {
            try
            {
                await history.GetHistoryAsync(symbol, "5Y", cancellationToken);
                _logger.LogInformation("Pre-warmed history for {Symbol}", symbol);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "History pre-warm failed for {Symbol}", symbol);
            }
        }


        // Idempotent: if all demo users already exist with transactions, skip.
        var existingCount = await db.Set<ApplicationUser>()
            .Where(u => DemoEmails.Contains(u.Email!))
            .CountAsync(cancellationToken);
        var existingTransactions = await db.Transactions
            .Where(t => db.Set<ApplicationUser>().Any(u => u.Id == t.UserId && DemoEmails.Contains(u.Email!)))
            .CountAsync(cancellationToken);
        if (existingCount == DemoEmails.Length && existingTransactions > 0)
        {
            _logger.LogInformation("Demo data already seeded — skipping.");
            return;
        }

        // Create users (if missing).
        var userIds = new List<string>();
        foreach (var email in DemoEmails)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    DisplayName = email.Split('@')[0],
                    CashBalance = 10_000m,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                };
                var create = await userManager.CreateAsync(user, "Demo1234");
                if (!create.Succeeded)
                {
                    _logger.LogError("Failed to create {Email}: {Errors}", email,
                        string.Join("; ", create.Errors.Select(e => e.Description)));
                    continue;
                }
                await userManager.AddToRoleAsync(user, RoleNames.User);
                _logger.LogInformation("Created demo user {Email}", email);
            }
            userIds.Add(user.Id);
        }

        // Generate transactions over the past 14 months.
        var random = new Random(42); // deterministic
        foreach (var userId in userIds)
        {
            // Skip if this user already has transactions.
            var hasTx = await db.Transactions.AnyAsync(t => t.UserId == userId, cancellationToken);
            if (hasTx) continue;

            var user = await db.Set<ApplicationUser>().FirstAsync(u => u.Id == userId, cancellationToken);
            var cash = user.CashBalance;
            var positions = new Dictionary<string, (decimal Qty, decimal Avg)>(StringComparer.OrdinalIgnoreCase);

            // 15 transactions, spaced over 14 months back from today.
            var now = DateTime.UtcNow;
            var earliest = now.AddMonths(-14);
            for (int i = 0; i < 15; i++)
            {
                var fraction = i / 14.0;
                var when = earliest.AddTicks((long)((now - earliest).Ticks * fraction))
                    .AddHours(random.Next(0, 8))
                    .AddMinutes(random.Next(0, 60));

                var symbol = DemoSymbols[random.Next(DemoSymbols.Length)];
                var isBuy = positions.GetValueOrDefault(symbol).Qty == 0 || random.NextDouble() > 0.3;

                // Pull a plausible historical price from the cache if we have one for that date.
                var price = await PickPlausiblePriceAsync(db, symbol, DateOnly.FromDateTime(when), random, cancellationToken);
                if (price <= 0) continue;

                if (isBuy)
                {
                    var maxQty = (int)Math.Floor(Math.Min(cash, 1500m) / price);
                    if (maxQty <= 0) continue;
                    var qty = (decimal)random.Next(1, Math.Max(2, maxQty));
                    var total = qty * price;
                    if (total > cash) continue;

                    db.Transactions.Add(new Transaction
                    {
                        UserId = userId,
                        Symbol = symbol,
                        Type = TransactionType.Buy,
                        Quantity = qty,
                        PricePerShare = price,
                        Fees = 0m,
                        TotalAmount = total,
                        ExecutedAt = when,
                    });

                    var existing = positions.GetValueOrDefault(symbol);
                    var newQty = existing.Qty + qty;
                    var newAvg = newQty == 0 ? 0m : (existing.Qty * existing.Avg + total) / newQty;
                    positions[symbol] = (newQty, newAvg);

                    EnsurePositionRow(db, userId, symbol, newQty, newAvg, total, when, isClosed: false);
                    cash -= total;
                }
                else
                {
                    var (heldQty, heldAvg) = positions[symbol];
                    if (heldQty <= 0) continue;
                    var qty = (decimal)random.Next(1, (int)Math.Floor(heldQty) + 1);
                    var total = qty * price;
                    var realized = (price - heldAvg) * qty;

                    db.Transactions.Add(new Transaction
                    {
                        UserId = userId,
                        Symbol = symbol,
                        Type = TransactionType.Sell,
                        Quantity = qty,
                        PricePerShare = price,
                        Fees = 0m,
                        TotalAmount = total,
                        ExecutedAt = when,
                        RealizedPl = realized,
                    });

                    var newQty = heldQty - qty;
                    positions[symbol] = (newQty, heldAvg);
                    EnsurePositionRow(db, userId, symbol, newQty, heldAvg, totalInvested: heldQty * heldAvg, when, isClosed: newQty == 0, realizedDelta: realized);
                    cash += total;
                }
            }

            user.CashBalance = cash;
            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded transactions for user {Email}", user.Email);

            // Backfill snapshots so the Analytics chart has data.
            await snapshots.BackfillAsync(userId, cancellationToken);
        }
    }

    private static void EnsurePositionRow(AppDbContext db, string userId, string symbol, decimal newQty, decimal avg, decimal totalInvested, DateTime now, bool isClosed, decimal realizedDelta = 0m)
    {
        var existing = db.Positions.Local.FirstOrDefault(p => p.UserId == userId && p.Symbol == symbol)
                       ?? db.Positions.FirstOrDefault(p => p.UserId == userId && p.Symbol == symbol);
        if (existing is null)
        {
            db.Positions.Add(new Position
            {
                UserId = userId,
                Symbol = symbol,
                Quantity = newQty,
                AverageCost = avg,
                TotalInvested = totalInvested,
                RealizedPlLifetime = realizedDelta,
                FirstPurchasedAt = now,
                LastTransactionAt = now,
                IsClosed = isClosed,
            });
        }
        else
        {
            existing.Quantity = newQty;
            existing.AverageCost = avg;
            existing.TotalInvested = Math.Max(existing.TotalInvested, totalInvested);
            existing.RealizedPlLifetime += realizedDelta;
            existing.LastTransactionAt = now;
            existing.IsClosed = isClosed;
        }
    }

    private static async Task<decimal> PickPlausiblePriceAsync(AppDbContext db, string symbol, DateOnly date, Random random, CancellationToken cancellationToken)
    {
        // Prefer the close from HistoricalPrices on or before this date.
        var close = await db.HistoricalPrices
            .Where(h => h.Symbol == symbol && h.Date <= date)
            .OrderByDescending(h => h.Date)
            .Select(h => (decimal?)h.Close)
            .FirstOrDefaultAsync(cancellationToken);
        if (close is not null) return close.Value;

        // Fallback to a coarse synthetic price (only used if we have no historical data
        // for this symbol yet — which shouldn't happen after Epic 3 caches 1Y of data).
        return symbol switch
        {
            "AAPL" => 200m + (decimal)random.NextDouble() * 100m,
            "MSFT" => 350m + (decimal)random.NextDouble() * 80m,
            "GOOGL" => 150m + (decimal)random.NextDouble() * 60m,
            "AMZN" => 150m + (decimal)random.NextDouble() * 100m,
            "TSLA" => 200m + (decimal)random.NextDouble() * 200m,
            "NVDA" => 100m + (decimal)random.NextDouble() * 150m,
            "META" => 300m + (decimal)random.NextDouble() * 200m,
            "AMD" => 100m + (decimal)random.NextDouble() * 200m,
            _ => 100m,
        };
    }
}
