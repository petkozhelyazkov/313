using System.ComponentModel.DataAnnotations;

namespace Trading313.Api.Dtos.Watchlist;

public class AddToWatchlistRequest
{
    [Required, MaxLength(16)]
    public string Symbol { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MaxLength(50)]
    public string? ListName { get; set; }
}

public class RenameWatchlistRequest
{
    [Required, MaxLength(50)]
    public string NewName { get; set; } = string.Empty;
}

public class UpdateWatchlistNotesRequest
{
    [MaxLength(500)]
    public string? Notes { get; set; }
}
