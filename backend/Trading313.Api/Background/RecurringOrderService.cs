using Microsoft.EntityFrameworkCore;
using Trading313.Api.Data;
using Trading313.Api.Dtos.Portfolio;
using Trading313.Api.Services.MarketData;
using Trading313.Api.Services.Portfolio;
using Trading313.Api.Services.RecurringOrders;

namespace Trading313.Api.Background;

/// <summary>
/// Evaluates active recurring DCA rules every 5 minutes and fires buys
/// for any rule whose <c>NextRunAt</c> is due.
/// </summary>
public class RecurringOrderService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecurringOrderService> _logger;

    public RecurringOrderService(IServiceScopeFactory scopeFactory, ILogger<RecurringOrderService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RecurringOrderService started; tick every {Minutes}m", TickInterval.TotalMinutes);

        try { await Task.Delay(TimeSpan.FromSeconds(120), stoppingToken); }
        catch (OperationCanceledException) { return; }

        using var timer = new PeriodicTimer(TickInterval);
        while (await SafeWaitAsync(timer, stoppingToken))
        {
            try { await TickAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "RecurringOrderService tick threw"); }
        }
    }

    private static async Task<bool> SafeWaitAsync(PeriodicTimer timer, CancellationToken cancellationToken)
    {
        try { return await timer.WaitForNextTickAsync(cancellationToken); }
        catch (OperationCanceledException) { return false; }
    }

    private async Task TickAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var portfolio = scope.ServiceProvider.GetRequiredService<IPortfolioService>();
        var quotes = scope.ServiceProvider.GetRequiredService<IQuoteService>();

        var now = DateTime.UtcNow;
        var due = await db.RecurringOrders
            .Where(r => r.IsActive && r.NextRunAt <= now)
            .ToListAsync(cancellationToken);
        if (due.Count == 0) return;

        int fired = 0;
        foreach (var rule in due)
        {
            try
            {
                var quote = await quotes.GetQuoteAsync(rule.Symbol, cancellationToken);
                if (quote is null || quote.Price <= 0m)
                {
                    rule.FailedRuns++;
                    rule.LastFailureReason = "Price unavailable.";
                    rule.NextRunAt = RecurringOrdersService.ComputeNextRun(now, rule.Frequency);
                    rule.LastRunAt = now;
                    continue;
                }

                var qty = Math.Round(rule.CashAmount / quote.Price, 6, MidpointRounding.ToZero);
                if (qty <= 0m)
                {
                    rule.FailedRuns++;
                    rule.LastFailureReason = $"Cash {rule.CashAmount:C} too small to buy any shares at {quote.Price:C}.";
                    rule.NextRunAt = RecurringOrdersService.ComputeNextRun(now, rule.Frequency);
                    rule.LastRunAt = now;
                    continue;
                }

                var result = await portfolio.BuyAsync(rule.UserId, new BuyRequest
                {
                    Symbol = rule.Symbol,
                    Quantity = qty,
                    Notes = $"DCA: ${rule.CashAmount:F2} {rule.Frequency}",
                }, cancellationToken);

                if (result.Succeeded)
                {
                    rule.SuccessfulRuns++;
                    rule.LastFailureReason = null;
                    fired++;
                }
                else
                {
                    rule.FailedRuns++;
                    rule.LastFailureReason = result.ErrorMessage;
                }
                rule.LastRunAt = now;
                rule.NextRunAt = RecurringOrdersService.ComputeNextRun(now, rule.Frequency);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Recurring rule {RuleId} threw", rule.Id);
                rule.FailedRuns++;
                rule.LastFailureReason = ex.Message;
                rule.LastRunAt = now;
                rule.NextRunAt = RecurringOrdersService.ComputeNextRun(now, rule.Frequency);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        if (fired > 0)
            _logger.LogInformation("Recurring DCA: {Fired}/{Total} rules fired this tick", fired, due.Count);
    }
}
