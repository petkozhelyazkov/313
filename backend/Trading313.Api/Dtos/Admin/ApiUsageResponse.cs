namespace Trading313.Api.Dtos.Admin;

public record ApiUsageResponse(
    ApiUsageWindow Today,
    ApiUsageWindow LastHour,
    IReadOnlyList<ApiUsageCallEntry> RecentCalls);

public record ApiUsageWindow(int Count, int Quota, double PercentUsed);

public record ApiUsageCallEntry(
    long Id,
    string Endpoint,
    string? Symbols,
    DateTime RequestedAt,
    int StatusCode,
    long ResponseTimeMs);
