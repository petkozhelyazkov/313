namespace Trading313.Api.Dtos.Analytics;

public record SnapshotPoint(
    DateOnly Date,
    decimal TotalValue,
    decimal CashBalance,
    decimal HoldingsValue,
    decimal TotalInvested,
    decimal UnrealizedPl,
    decimal? Benchmark = null);

public record AllocationSlice(
    string Symbol,
    decimal Value,
    decimal Weight,
    decimal Quantity);

public record ReturnsRow(
    string Symbol,
    decimal UnrealizedPl,
    decimal RealizedPl,
    decimal TotalPl,
    decimal? TotalPlPct);

public record SectorSlice(
    string Sector,
    decimal Value,
    decimal Weight,
    int Symbols);

public record RiskMetricsResponse(
    decimal? Beta,
    decimal? AnnualizedVolatility,
    decimal? SharpeRatio,
    decimal? MaxDrawdown,
    int DataPoints);

public record DiversificationResponse(
    int Score,
    int PositionsCount,
    int SectorsCount,
    decimal LargestPositionPct,
    decimal LargestSectorPct,
    string Verdict,
    IReadOnlyList<string> Suggestions);

public record AdvancedMetricsResponse(
    decimal? TimeWeightedReturn,
    decimal? MoneyWeightedReturn,
    decimal? SortinoRatio,
    decimal? BestDayReturn,
    DateOnly? BestDayDate,
    decimal? WorstDayReturn,
    DateOnly? WorstDayDate,
    int PositiveDays,
    int NegativeDays,
    decimal? WinRate,
    decimal? AverageDailyReturn,
    int DataPoints,
    string Range);
