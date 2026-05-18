using Microsoft.EntityFrameworkCore;
using Trading313.Api.Data;
using Trading313.Api.Domain.Entities;
using Trading313.Api.Dtos.Stocks;
using Trading313.Api.Dtos.Watchlist;
using Trading313.Api.Services.MarketData;
using Trading313.Api.Services.Stocks;

namespace Trading313.Api.Services.Watchlist;

public class WatchlistService : IWatchlistService
{
    private const string DefaultList = "Default";
    private readonly AppDbContext _db;
    private readonly IStockService _stocks;
    private readonly IQuoteService _quotes;

    public WatchlistService(AppDbContext db, IStockService stocks, IQuoteService quotes)
    {
        _db = db;
        _stocks = stocks;
        _quotes = quotes;
    }

    private static string Normalize(string? listName)
        => string.IsNullOrWhiteSpace(listName) ? DefaultList : listName.Trim();

    public async Task<IReadOnlyList<WatchlistItemDto>> GetAllAsync(string userId, string? listName, CancellationToken cancellationToken = default)
    {
        var query = _db.WatchlistItems.Where(w => w.UserId == userId);
        if (!string.IsNullOrWhiteSpace(listName))
        {
            var name = listName.Trim();
            query = query.Where(w => w.ListName == name);
        }

        var items = await query.OrderByDescending(w => w.AddedAt).ToListAsync(cancellationToken);
        if (items.Count == 0) return Array.Empty<WatchlistItemDto>();

        var quotes = new Dictionary<string, QuoteResponse?>(StringComparer.OrdinalIgnoreCase);
        foreach (var symbol in items.Select(i => i.Symbol).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            quotes[symbol] = await _quotes.GetQuoteAsync(symbol, cancellationToken);
        }

        var symbolKeys = items.Select(i => i.Symbol).Distinct().ToList();
        var stockMeta = await _db.Stocks
            .Where(s => symbolKeys.Contains(s.Symbol))
            .Select(s => new { s.Symbol, s.LogoUrl, s.Name })
            .ToDictionaryAsync(x => x.Symbol, cancellationToken);

        return items.Select(i =>
        {
            stockMeta.TryGetValue(i.Symbol, out var meta);
            return new WatchlistItemDto(
                Id: i.Id,
                Symbol: i.Symbol,
                Notes: i.Notes,
                AddedAt: i.AddedAt,
                Quote: quotes.TryGetValue(i.Symbol, out var q) ? q : null,
                LogoUrl: meta?.LogoUrl,
                Name: meta?.Name,
                ListName: i.ListName);
        }).ToList();
    }

    public async Task<IReadOnlyList<WatchlistSummaryDto>> GetListsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var groups = await _db.WatchlistItems
            .Where(w => w.UserId == userId)
            .GroupBy(w => w.ListName)
            .Select(g => new WatchlistSummaryDto(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        if (groups.All(g => g.ListName != DefaultList))
        {
            groups.Insert(0, new WatchlistSummaryDto(DefaultList, 0));
        }

        return groups.OrderBy(g => g.ListName == DefaultList ? 0 : 1).ThenBy(g => g.ListName).ToList();
    }

    public async Task<WatchlistOutcome> AddAsync(string userId, string symbol, string? notes, string? listName, CancellationToken cancellationToken = default)
    {
        var sym = symbol.Trim().ToUpperInvariant();
        var list = Normalize(listName);

        var stock = await _stocks.GetBySymbolAsync(sym, cancellationToken);
        if (stock is null)
            return WatchlistOutcome.Fail(WatchlistFailureKind.SymbolNotResolved, $"Unknown symbol '{sym}'.");

        var existing = await _db.WatchlistItems
            .FirstOrDefaultAsync(w => w.UserId == userId && w.Symbol == sym && w.ListName == list, cancellationToken);
        if (existing is not null)
            return WatchlistOutcome.Fail(WatchlistFailureKind.AlreadyExists, $"{sym} is already in your '{list}' watchlist.");

        var item = new WatchlistItem
        {
            UserId = userId,
            Symbol = sym,
            ListName = list,
            Notes = notes,
            AddedAt = DateTime.UtcNow,
        };
        _db.WatchlistItems.Add(item);
        await _db.SaveChangesAsync(cancellationToken);

        var quote = await _quotes.GetQuoteAsync(sym, cancellationToken);
        var dto = new WatchlistItemDto(item.Id, sym, item.Notes, item.AddedAt, quote, ListName: list);
        return WatchlistOutcome.Ok(dto);
    }

    public async Task<WatchlistOutcome> RemoveAsync(string userId, string symbol, string? listName, CancellationToken cancellationToken = default)
    {
        var sym = symbol.Trim().ToUpperInvariant();
        var query = _db.WatchlistItems.Where(w => w.UserId == userId && w.Symbol == sym);
        if (!string.IsNullOrWhiteSpace(listName))
        {
            var list = listName.Trim();
            query = query.Where(w => w.ListName == list);
        }

        var item = await query.FirstOrDefaultAsync(cancellationToken);
        if (item is null)
            return WatchlistOutcome.Fail(WatchlistFailureKind.NotFound, $"{sym} is not in your watchlist.");

        _db.WatchlistItems.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
        return WatchlistOutcome.Ok();
    }

    public async Task<WatchlistOutcome> UpdateNotesAsync(string userId, string symbol, string? notes, string? listName, CancellationToken cancellationToken = default)
    {
        var sym = symbol.Trim().ToUpperInvariant();
        var query = _db.WatchlistItems.Where(w => w.UserId == userId && w.Symbol == sym);
        if (!string.IsNullOrWhiteSpace(listName))
        {
            var list = listName.Trim();
            query = query.Where(w => w.ListName == list);
        }
        var item = await query.FirstOrDefaultAsync(cancellationToken);
        if (item is null)
            return WatchlistOutcome.Fail(WatchlistFailureKind.NotFound, $"{sym} is not in your watchlist.");

        item.Notes = notes;
        await _db.SaveChangesAsync(cancellationToken);

        var quote = await _quotes.GetQuoteAsync(sym, cancellationToken);
        var dto = new WatchlistItemDto(item.Id, sym, item.Notes, item.AddedAt, quote, ListName: item.ListName);
        return WatchlistOutcome.Ok(dto);
    }

    public async Task<WatchlistOutcome> RenameListAsync(string userId, string oldName, string newName, CancellationToken cancellationToken = default)
    {
        var oldList = oldName.Trim();
        var newList = newName.Trim();
        if (string.IsNullOrWhiteSpace(newList))
            return WatchlistOutcome.Fail(WatchlistFailureKind.SymbolNotResolved, "List name cannot be empty.");
        if (oldList == newList)
            return WatchlistOutcome.Ok();

        var conflict = await _db.WatchlistItems.AnyAsync(w => w.UserId == userId && w.ListName == newList, cancellationToken);
        if (conflict)
            return WatchlistOutcome.Fail(WatchlistFailureKind.AlreadyExists, $"A list named '{newList}' already exists.");

        var items = await _db.WatchlistItems
            .Where(w => w.UserId == userId && w.ListName == oldList)
            .ToListAsync(cancellationToken);
        foreach (var i in items) i.ListName = newList;
        await _db.SaveChangesAsync(cancellationToken);
        return WatchlistOutcome.Ok();
    }

    public async Task<WatchlistOutcome> DeleteListAsync(string userId, string listName, CancellationToken cancellationToken = default)
    {
        var list = listName.Trim();
        if (list == DefaultList)
            return WatchlistOutcome.Fail(WatchlistFailureKind.AlreadyExists, "The 'Default' list cannot be deleted.");

        var items = await _db.WatchlistItems
            .Where(w => w.UserId == userId && w.ListName == list)
            .ToListAsync(cancellationToken);
        if (items.Count == 0)
            return WatchlistOutcome.Fail(WatchlistFailureKind.NotFound, $"List '{list}' not found.");

        _db.WatchlistItems.RemoveRange(items);
        await _db.SaveChangesAsync(cancellationToken);
        return WatchlistOutcome.Ok();
    }

    public Task<bool> ContainsAsync(string userId, string symbol, CancellationToken cancellationToken = default)
    {
        var sym = symbol.Trim().ToUpperInvariant();
        return _db.WatchlistItems.AnyAsync(w => w.UserId == userId && w.Symbol == sym, cancellationToken);
    }
}
