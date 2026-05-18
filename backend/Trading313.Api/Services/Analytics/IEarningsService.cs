using Trading313.Api.Dtos.Analytics;

namespace Trading313.Api.Services.Analytics;

public interface IEarningsService
{
    Task<IReadOnlyList<EarningsCalendarItem>> GetUpcomingForUserAsync(string userId, int daysAhead, CancellationToken cancellationToken = default);
}
