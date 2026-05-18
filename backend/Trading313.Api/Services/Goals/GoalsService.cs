using Microsoft.EntityFrameworkCore;
using Trading313.Api.Data;
using Trading313.Api.Domain.Entities;
using Trading313.Api.Domain.Enums;
using Trading313.Api.Dtos.Goals;
using Trading313.Api.Services.Dividends;
using Trading313.Api.Services.MarketData;

namespace Trading313.Api.Services.Goals;

public interface IGoalsService
{
    Task<IReadOnlyList<GoalDto>> GetForUserAsync(string userId, CancellationToken cancellationToken);
    Task<(bool ok, string? error, GoalDto? value)> CreateAsync(string userId, CreateGoalRequest request, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(string userId, long id, UpdateGoalRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(string userId, long id, CancellationToken cancellationToken);
}

public class GoalsService : IGoalsService
{
    private readonly AppDbContext _db;
    private readonly IQuoteService _quotes;
    private readonly IDividendsService _dividends;

    public GoalsService(AppDbContext db, IQuoteService quotes, IDividendsService dividends)
    {
        _db = db;
        _quotes = quotes;
        _dividends = dividends;
    }

    public async Task<IReadOnlyList<GoalDto>> GetForUserAsync(string userId, CancellationToken cancellationToken)
    {
        var goals = await _db.Goals
            .Where(g => g.UserId == userId)
            .OrderBy(g => g.IsCompleted)
            .ThenByDescending(g => g.CreatedAt)
            .ToListAsync(cancellationToken);
        if (goals.Count == 0) return Array.Empty<GoalDto>();

        // Resolve "current" per type. We do this once and reuse across goals.
        decimal? portfolioValue = null;
        decimal? totalReturn = null;
        decimal? lifetimeDividends = null;

        var list = new List<GoalDto>(goals.Count);
        foreach (var g in goals)
        {
            decimal current = g.Type switch
            {
                GoalType.PortfolioValue => portfolioValue ??= await ComputePortfolioValueAsync(userId, cancellationToken),
                GoalType.TotalReturn => totalReturn ??= await ComputeTotalReturnAsync(userId, cancellationToken),
                GoalType.DividendIncome => lifetimeDividends ??= await ComputeLifetimeDividendsAsync(userId, cancellationToken),
                _ => 0m,
            };

            var pct = g.TargetAmount <= 0m
                ? 0m
                : Math.Round(Math.Min(current / g.TargetAmount * 100m, 999m), 2);

            list.Add(new GoalDto(
                Id: g.Id,
                Type: g.Type.ToString(),
                TargetAmount: g.TargetAmount,
                CurrentAmount: Math.Round(current, 4),
                ProgressPct: pct,
                Title: g.Title,
                DueDate: g.DueDate,
                CreatedAt: g.CreatedAt,
                IsCompleted: g.IsCompleted,
                CompletedAt: g.CompletedAt));
        }
        return list;
    }

    public async Task<(bool, string?, GoalDto?)> CreateAsync(string userId, CreateGoalRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<GoalType>(request.Type, ignoreCase: true, out var type))
            return (false, $"Unknown goal type '{request.Type}'.", null);
        if (request.TargetAmount <= 0)
            return (false, "Target amount must be positive.", null);

        var entity = new Goal
        {
            UserId = userId,
            Type = type,
            TargetAmount = request.TargetAmount,
            Title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim(),
            DueDate = request.DueDate,
            CreatedAt = DateTime.UtcNow,
        };
        _db.Goals.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        var all = await GetForUserAsync(userId, cancellationToken);
        return (true, null, all.First(g => g.Id == entity.Id));
    }

    public async Task<bool> UpdateAsync(string userId, long id, UpdateGoalRequest request, CancellationToken cancellationToken)
    {
        var row = await _db.Goals.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId, cancellationToken);
        if (row is null) return false;
        if (request.TargetAmount is { } amt && amt > 0) row.TargetAmount = amt;
        if (request.Title is not null) row.Title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim();
        if (request.DueDate is { } d) row.DueDate = d;
        if (request.IsCompleted is { } c)
        {
            row.IsCompleted = c;
            row.CompletedAt = c ? DateTime.UtcNow : null;
        }
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(string userId, long id, CancellationToken cancellationToken)
    {
        var row = await _db.Goals.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId, cancellationToken);
        if (row is null) return false;
        _db.Goals.Remove(row);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<decimal> ComputePortfolioValueAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        var cash = user?.CashBalance ?? 0m;
        var positions = await _db.Positions
            .Where(p => p.UserId == userId && !p.IsClosed && p.Quantity > 0)
            .ToListAsync(cancellationToken);
        decimal holdings = 0m;
        foreach (var p in positions)
        {
            var q = await _quotes.GetQuoteAsync(p.Symbol, cancellationToken);
            holdings += p.Quantity * (q?.Price ?? p.AverageCost);
        }
        return cash + holdings;
    }

    private async Task<decimal> ComputeTotalReturnAsync(string userId, CancellationToken cancellationToken)
    {
        var realized = await _db.Transactions
            .Where(t => t.UserId == userId && t.RealizedPl != null)
            .SumAsync(t => t.RealizedPl ?? 0m, cancellationToken);

        var positions = await _db.Positions
            .Where(p => p.UserId == userId && !p.IsClosed && p.Quantity > 0)
            .ToListAsync(cancellationToken);
        decimal unrealized = 0m;
        foreach (var p in positions)
        {
            var q = await _quotes.GetQuoteAsync(p.Symbol, cancellationToken);
            var price = q?.Price ?? p.AverageCost;
            unrealized += (price - p.AverageCost) * p.Quantity;
        }
        return realized + unrealized;
    }

    private async Task<decimal> ComputeLifetimeDividendsAsync(string userId, CancellationToken cancellationToken)
    {
        var summary = await _dividends.GetSummaryAsync(userId, cancellationToken);
        return summary.LifetimeReceived;
    }
}
