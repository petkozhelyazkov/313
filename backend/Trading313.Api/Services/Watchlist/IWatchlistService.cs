using Trading313.Api.Dtos.Watchlist;

namespace Trading313.Api.Services.Watchlist;

public interface IWatchlistService
{
    Task<IReadOnlyList<WatchlistItemDto>> GetAllAsync(string userId, string? listName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WatchlistSummaryDto>> GetListsAsync(string userId, CancellationToken cancellationToken = default);
    Task<WatchlistOutcome> AddAsync(string userId, string symbol, string? notes, string? listName, CancellationToken cancellationToken = default);
    Task<WatchlistOutcome> RemoveAsync(string userId, string symbol, string? listName, CancellationToken cancellationToken = default);
    Task<WatchlistOutcome> UpdateNotesAsync(string userId, string symbol, string? notes, string? listName, CancellationToken cancellationToken = default);
    Task<WatchlistOutcome> RenameListAsync(string userId, string oldName, string newName, CancellationToken cancellationToken = default);
    Task<WatchlistOutcome> DeleteListAsync(string userId, string listName, CancellationToken cancellationToken = default);
    Task<bool> ContainsAsync(string userId, string symbol, CancellationToken cancellationToken = default);
}

public enum WatchlistFailureKind
{
    None,
    AlreadyExists,
    NotFound,
    SymbolNotResolved,
}

public class WatchlistOutcome
{
    public bool Succeeded { get; private init; }
    public WatchlistFailureKind FailureKind { get; private init; }
    public string? ErrorMessage { get; private init; }
    public WatchlistItemDto? Value { get; private init; }

    public static WatchlistOutcome Ok(WatchlistItemDto? value = null)
        => new() { Succeeded = true, Value = value };

    public static WatchlistOutcome Fail(WatchlistFailureKind kind, string message)
        => new() { Succeeded = false, FailureKind = kind, ErrorMessage = message };
}
