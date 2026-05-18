using Microsoft.AspNetCore.SignalR;
using Trading313.Api.Dtos.Stocks;

namespace Trading313.Api.Realtime;

public interface IPricePublisher
{
    Task PublishAsync(IEnumerable<QuoteResponse> quotes, CancellationToken cancellationToken = default);
}

/// <summary>
/// Fans quote updates out to PriceHub clients. Each client only receives ticks
/// for the symbols it explicitly subscribed to.
/// </summary>
public class PricePublisher : IPricePublisher
{
    private readonly IHubContext<PriceHub> _hub;
    private readonly ILogger<PricePublisher> _logger;

    public PricePublisher(IHubContext<PriceHub> hub, ILogger<PricePublisher> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task PublishAsync(IEnumerable<QuoteResponse> quotes, CancellationToken cancellationToken = default)
    {
        var list = quotes.ToList();
        if (list.Count == 0) return;

        var subscriptions = PriceHub.Snapshot();
        if (subscriptions.Count == 0) return;

        foreach (var (connectionId, syms) in subscriptions)
        {
            List<QuoteResponse> matching;
            lock (syms)
            {
                matching = list.Where(q => syms.Contains(q.Symbol)).ToList();
            }
            if (matching.Count == 0) continue;

            try
            {
                await _hub.Clients.Client(connectionId).SendAsync("priceUpdate", matching, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to send price tick to {ConnectionId}", connectionId);
            }
        }
    }
}
