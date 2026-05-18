namespace Trading313.Api.Domain.Entities;

/// <summary>
/// A symbol a user wants to keep an eye on. No effect on cash or positions.
/// </summary>
public class WatchlistItem
{
    public long Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string ListName { get; set; } = "Default";
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
}
