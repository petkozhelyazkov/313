using Trading313.Api.Services.Digests;

namespace Trading313.Api.Background;

/// <summary>
/// Generates a weekly digest for every opted-in user. Fires once on startup
/// (catch-up) then every Monday at 09:00 UTC.
/// </summary>
public class EmailDigestBackgroundService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);
    private const int SendHourUtc = 9;
    private const DayOfWeek SendDay = DayOfWeek.Monday;

    private readonly IServiceProvider _services;
    private readonly ILogger<EmailDigestBackgroundService> _logger;
    private DateTime _lastRunUtc = DateTime.MinValue;

    public EmailDigestBackgroundService(IServiceProvider services, ILogger<EmailDigestBackgroundService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var shouldFire = now.DayOfWeek == SendDay
                                 && now.Hour == SendHourUtc
                                 && (now - _lastRunUtc) > TimeSpan.FromHours(23);
                if (shouldFire)
                {
                    await using var scope = _services.CreateAsyncScope();
                    var svc = scope.ServiceProvider.GetRequiredService<IEmailDigestService>();
                    var count = await svc.RunWeeklyForAllUsersAsync(stoppingToken);
                    _logger.LogInformation("Weekly digest run generated {Count} digests", count);
                    _lastRunUtc = now;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Digest background service tick failed");
            }
            try { await Task.Delay(CheckInterval, stoppingToken); }
            catch (TaskCanceledException) { break; }
        }
    }
}
