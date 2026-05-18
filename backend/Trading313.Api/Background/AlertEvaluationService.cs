using Microsoft.EntityFrameworkCore;
using Trading313.Api.Data;
using Trading313.Api.Domain.Entities;

namespace Trading313.Api.Background;

/// <summary>
/// Evaluates active PriceAlerts every 60s against the latest PriceCache.
/// Marks alerts as Triggered (with TriggeredAt + TriggeredPrice).
/// Frontend polls /api/alerts and surfaces unacknowledged triggered alerts.
/// </summary>
public class AlertEvaluationService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(60);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AlertEvaluationService> _logger;

    public AlertEvaluationService(IServiceScopeFactory scopeFactory, ILogger<AlertEvaluationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AlertEvaluationService started; tick every {Seconds}s", TickInterval.TotalSeconds);
        try { await Task.Delay(TimeSpan.FromSeconds(120), stoppingToken); }
        catch (OperationCanceledException) { return; }

        using var timer = new PeriodicTimer(TickInterval);
        while (await SafeWait(timer, stoppingToken))
        {
            try { await TickAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "AlertEvaluationService tick threw"); }
        }
    }

    private static async Task<bool> SafeWait(PeriodicTimer timer, CancellationToken cancellationToken)
    {
        try { return await timer.WaitForNextTickAsync(cancellationToken); }
        catch (OperationCanceledException) { return false; }
    }

    private async Task TickAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var active = await db.PriceAlerts
            .Where(a => a.Status == AlertStatus.Active)
            .ToListAsync(cancellationToken);
        if (active.Count == 0) return;

        var symbols = active.Select(a => a.Symbol).Distinct().ToList();
        var prices = await db.PriceCache
            .Where(p => symbols.Contains(p.Symbol))
            .ToDictionaryAsync(p => p.Symbol, p => p.Price, cancellationToken);

        int triggered = 0;
        foreach (var a in active)
        {
            if (!prices.TryGetValue(a.Symbol, out var price)) continue;
            var fire = a.Direction switch
            {
                AlertDirection.Above => price >= a.TriggerPrice,
                AlertDirection.Below => price <= a.TriggerPrice,
                _ => false,
            };
            if (!fire) continue;
            a.Status = AlertStatus.Triggered;
            a.TriggeredAt = DateTime.UtcNow;
            a.TriggeredPrice = price;
            triggered++;
        }

        if (triggered > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Alerts: {Triggered}/{Active} fired this tick", triggered, active.Count);
        }
    }
}
