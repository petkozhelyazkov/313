namespace Trading313.Api.Background;

/// <summary>
/// US market hours approximation. We accept the union of EDT and EST trading
/// windows (13:30–21:00 UTC) instead of doing proper DST handling — that's
/// noted as future work in the thesis.
/// </summary>
public static class MarketHoursClock
{
    public static bool IsLikelyOpen(DateTime utcNow)
    {
        if (utcNow.DayOfWeek == DayOfWeek.Saturday || utcNow.DayOfWeek == DayOfWeek.Sunday)
            return false;

        var minutes = utcNow.Hour * 60 + utcNow.Minute;
        const int openMinutes = 13 * 60 + 30;   // 13:30 UTC (covers EDT open at 13:30, EST open at 14:30)
        const int closeMinutes = 21 * 60;       // 21:00 UTC (covers EDT close at 20:00, EST close at 21:00)
        return minutes >= openMinutes && minutes <= closeMinutes;
    }
}
