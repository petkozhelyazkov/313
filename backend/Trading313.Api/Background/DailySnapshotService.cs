using Trading313.Api.Services.Analytics;

namespace Trading313.Api.Background;

/// <summary>
/// Background service that computes a snapshot for every active user once daily at 23:00 UTC.
/// </summary>
public class DailySnapshotService : BackgroundService
{
    private static readonly TimeSpan RunAtUtcHour = TimeSpan.FromHours(23);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DailySnapshotService> _logger;

    public DailySnapshotService(IServiceScopeFactory scopeFactory, ILogger<DailySnapshotService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DailySnapshotService started; runs at {Hour}:00 UTC", RunAtUtcHour.Hours);

        while (!stoppingToken.IsCancellationRequested)
        {
            var wait = TimeUntilNextRun();
            try
            {
                await Task.Delay(wait, stoppingToken);
            }
            catch (OperationCanceledException) { return; }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<ISnapshotService>();
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var processed = await svc.RunDailyForAllUsersAsync(today, stoppingToken);
                sw.Stop();
                _logger.LogInformation("Daily snapshot job processed {Count} users in {Elapsed}ms", processed, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Daily snapshot job threw");
            }
        }
    }

    private static TimeSpan TimeUntilNextRun()
    {
        var nowUtc = DateTime.UtcNow;
        var todayRun = nowUtc.Date.AddHours(RunAtUtcHour.Hours);
        var nextRun = nowUtc < todayRun ? todayRun : todayRun.AddDays(1);
        return nextRun - nowUtc;
    }
}
