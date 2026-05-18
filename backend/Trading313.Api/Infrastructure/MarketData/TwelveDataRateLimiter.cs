using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Trading313.Api.Data;
using Trading313.Api.Domain.Entities;

namespace Trading313.Api.Infrastructure.MarketData;

/// <summary>
/// Two-bucket rate limiter for Twelve Data: per-minute (sliding window) +
/// per-UTC-day (counter with DB-backed initial value).
/// </summary>
public class TwelveDataRateLimiter
{
    private readonly TwelveDataOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TwelveDataRateLimiter> _logger;

    private readonly object _lock = new();
    private readonly Queue<DateTime> _recentCallsUtc = new();

    private int _todayCount = -1;     // -1 = not yet loaded from DB
    private DateOnly _todayDate;

    public TwelveDataRateLimiter(
        IOptions<TwelveDataOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<TwelveDataRateLimiter> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public int TodayCountSnapshot
    {
        get { lock (_lock) return Math.Max(_todayCount, 0); }
    }

    public int LastMinuteCountSnapshot
    {
        get
        {
            lock (_lock)
            {
                TrimMinuteWindow_NoLock(DateTime.UtcNow);
                return _recentCallsUtc.Count;
            }
        }
    }

    /// <summary>
    /// Checks both buckets. On success, reserves the slot (callers MUST proceed to make the call,
    /// then call <see cref="RecordCallAsync"/> with the outcome).
    /// </summary>
    public async Task AcquireOrThrowAsync(CancellationToken cancellationToken = default)
    {
        await LoadTodayCountIfNeededAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        lock (_lock)
        {
            if (today != _todayDate)
            {
                _todayDate = today;
                _todayCount = 0;
            }

            TrimMinuteWindow_NoLock(now);

            if (_recentCallsUtc.Count >= _options.RequestsPerMinute)
            {
                throw new TwelveDataRateLimitException(
                    $"Twelve Data per-minute quota reached ({_options.RequestsPerMinute}/min).");
            }
            if (_todayCount >= _options.RequestsPerDay)
            {
                throw new TwelveDataRateLimitException(
                    $"Twelve Data daily quota reached ({_options.RequestsPerDay}/day).");
            }

            _recentCallsUtc.Enqueue(now);
            _todayCount++;
        }
    }

    /// <summary>
    /// Persists the call to ApiUsageLog. Best-effort — logs and swallows DB errors so the
    /// caller can still return the actual response.
    /// </summary>
    public async Task RecordCallAsync(
        string endpoint,
        string? symbols,
        int statusCode,
        long responseTimeMs,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            int quotaToday;
            lock (_lock) quotaToday = Math.Max(_todayCount, 0);

            db.ApiUsageLog.Add(new ApiUsageLogEntry
            {
                Endpoint = endpoint,
                Symbols = symbols,
                RequestedAt = DateTime.UtcNow,
                StatusCode = statusCode,
                ResponseTimeMs = responseTimeMs,
                QuotaUsedToday = quotaToday,
            });
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist ApiUsageLog entry for {Endpoint}", endpoint);
        }
    }

    private async Task LoadTodayCountIfNeededAsync(CancellationToken cancellationToken)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        lock (_lock)
        {
            if (_todayCount >= 0 && _todayDate == today) return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var startOfDay = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var count = await db.ApiUsageLog.CountAsync(e => e.RequestedAt >= startOfDay, cancellationToken);

            lock (_lock)
            {
                _todayDate = today;
                _todayCount = count;
            }
            _logger.LogInformation("Twelve Data daily counter primed: {Count}/{Quota} for {Date}",
                count, _options.RequestsPerDay, today);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load today's Twelve Data usage count — defaulting to 0");
            lock (_lock)
            {
                _todayDate = today;
                _todayCount = 0;
            }
        }
    }

    private void TrimMinuteWindow_NoLock(DateTime now)
    {
        var cutoff = now.AddSeconds(-60);
        while (_recentCallsUtc.Count > 0 && _recentCallsUtc.Peek() < cutoff)
        {
            _recentCallsUtc.Dequeue();
        }
    }
}
