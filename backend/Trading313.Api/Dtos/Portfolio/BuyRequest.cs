using System.ComponentModel.DataAnnotations;

namespace Trading313.Api.Dtos.Portfolio;

public class BuyRequest
{
    [Required, MaxLength(16)]
    public string Symbol { get; set; } = string.Empty;

    /// <summary>Number of shares (supports up to 8 decimal places).</summary>
    [Range(0.00000001, double.MaxValue, ErrorMessage = "Quantity must be greater than 0.")]
    public decimal Quantity { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
