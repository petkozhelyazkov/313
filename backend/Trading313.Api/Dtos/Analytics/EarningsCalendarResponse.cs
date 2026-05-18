namespace Trading313.Api.Dtos.Analytics;

public record EarningsCalendarItem(
    string Symbol,
    string? CompanyName,
    string? LogoUrl,
    DateOnly ReportDate,
    string? Time,
    decimal? EpsEstimate,
    decimal? EpsActual,
    bool IsHeld,
    bool IsWatched);
