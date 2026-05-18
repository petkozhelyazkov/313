using Microsoft.EntityFrameworkCore;
using Trading313.Api.Data;
using Trading313.Api.Domain.Entities;
using Trading313.Api.Domain.Enums;
using Trading313.Api.Dtos.RecurringOrders;
using Trading313.Api.Services.Stocks;

namespace Trading313.Api.Services.RecurringOrders;

public interface IRecurringOrdersService
{
    Task<IReadOnlyList<RecurringOrderDto>> GetForUserAsync(string userId, CancellationToken cancellationToken);
    Task<(bool success, string? error, RecurringOrderDto? value)> CreateAsync(string userId, CreateRecurringOrderRequest request, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(string userId, long id, UpdateRecurringOrderRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(string userId, long id, CancellationToken cancellationToken);
}

public class RecurringOrdersService : IRecurringOrdersService
{
    private readonly AppDbContext _db;
    private readonly IStockService _stocks;

    public RecurringOrdersService(AppDbContext db, IStockService stocks)
    {
        _db = db;
        _stocks = stocks;
    }

    public static DateTime ComputeNextRun(DateTime from, RecurringFrequency freq) => freq switch
    {
        RecurringFrequency.Daily => from.AddDays(1),
        RecurringFrequency.Weekly => from.AddDays(7),
        RecurringFrequency.Biweekly => from.AddDays(14),
        RecurringFrequency.Monthly => from.AddMonths(1),
        _ => from.AddDays(7),
    };

    public async Task<IReadOnlyList<RecurringOrderDto>> GetForUserAsync(string userId, CancellationToken cancellationToken)
    {
        var rows = await _db.RecurringOrders
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.IsActive)
            .ThenBy(r => r.NextRunAt)
            .ToListAsync(cancellationToken);
        return rows.Select(Map).ToList();
    }

    public async Task<(bool, string?, RecurringOrderDto?)> CreateAsync(string userId, CreateRecurringOrderRequest request, CancellationToken cancellationToken)
    {
        var sym = request.Symbol.Trim().ToUpperInvariant();
        var stock = await _stocks.GetBySymbolAsync(sym, cancellationToken);
        if (stock is null) return (false, $"Unknown symbol '{sym}'.", null);
        if (request.CashAmount <= 0) return (false, "Cash amount must be positive.", null);
        if (!RecurringFrequencyParser.TryParse(request.Frequency, out var freq))
            return (false, $"Unknown frequency '{request.Frequency}'. Use Daily, Weekly, Biweekly, or Monthly.", null);

        var startAt = request.StartAt ?? DateTime.UtcNow;
        var entity = new RecurringOrder
        {
            UserId = userId,
            Symbol = sym,
            CashAmount = request.CashAmount,
            Frequency = freq,
            NextRunAt = startAt,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        _db.RecurringOrders.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null, Map(entity));
    }

    public async Task<bool> UpdateAsync(string userId, long id, UpdateRecurringOrderRequest request, CancellationToken cancellationToken)
    {
        var row = await _db.RecurringOrders.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, cancellationToken);
        if (row is null) return false;
        if (request.CashAmount is { } amt && amt > 0) row.CashAmount = amt;
        if (request.Frequency is not null && RecurringFrequencyParser.TryParse(request.Frequency, out var f)) row.Frequency = f;
        if (request.IsActive is { } a) row.IsActive = a;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(string userId, long id, CancellationToken cancellationToken)
    {
        var row = await _db.RecurringOrders.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, cancellationToken);
        if (row is null) return false;
        _db.RecurringOrders.Remove(row);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static RecurringOrderDto Map(RecurringOrder r) => new(
        r.Id, r.Symbol, r.CashAmount, r.Frequency.ToString(), r.NextRunAt, r.LastRunAt, r.IsActive,
        r.SuccessfulRuns, r.FailedRuns, r.LastFailureReason);
}
