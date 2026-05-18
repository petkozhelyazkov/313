using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Trading313.Api.Realtime;

/// <summary>
/// Real-time price-tick stream. Clients call <see cref="Subscribe"/> with a list of
/// symbols and receive `priceUpdate` events whenever the QuoteRefreshService refreshes
/// any of them. Authenticated for parity with REST, but the data is the same public
/// quote payload so anonymous would also be fine — gating keeps the connection count
/// tied to known users.
/// </summary>
[Authorize]
public class PriceHub : Hub
{
    // ConnectionId -> set of subscribed symbols (uppercase).
    private static readonly ConcurrentDictionary<string, HashSet<string>> Subscriptions = new();

    public Task Subscribe(string[] symbols)
    {
        var set = Subscriptions.GetOrAdd(Context.ConnectionId, _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        lock (set)
        {
            foreach (var s in symbols)
            {
                if (!string.IsNullOrWhiteSpace(s)) set.Add(s.Trim().ToUpperInvariant());
            }
        }
        return Task.CompletedTask;
    }

    public Task Unsubscribe(string[] symbols)
    {
        if (!Subscriptions.TryGetValue(Context.ConnectionId, out var set)) return Task.CompletedTask;
        lock (set)
        {
            foreach (var s in symbols)
            {
                if (!string.IsNullOrWhiteSpace(s)) set.Remove(s.Trim().ToUpperInvariant());
            }
        }
        return Task.CompletedTask;
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        Subscriptions.TryRemove(Context.ConnectionId, out _);
        return base.OnDisconnectedAsync(exception);
    }

    internal static IReadOnlyDictionary<string, HashSet<string>> Snapshot() => Subscriptions;
}
