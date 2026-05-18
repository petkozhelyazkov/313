namespace Trading313.Api.Services.Analytics;

public interface ISnapshotService
{
    /// <summary>Compute and upsert a snapshot for the given user/date.</summary>
    Task<SnapshotComputed> ComputeAndPersistAsync(string userId, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>Backfill missing snapshots from the user's earliest transaction date to today.</summary>
    Task<int> BackfillAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Run the daily snapshot job for every user.</summary>
    Task<int> RunDailyForAllUsersAsync(DateOnly date, CancellationToken cancellationToken = default);
}

public record SnapshotComputed(
    DateOnly Date,
    decimal CashBalance,
    decimal HoldingsValue,
    decimal TotalValue,
    decimal TotalInvested,
    decimal UnrealizedPl);
