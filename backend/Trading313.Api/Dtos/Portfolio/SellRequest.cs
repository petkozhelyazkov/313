using System.ComponentModel.DataAnnotations;

namespace Trading313.Api.Dtos.Portfolio;

public class SellRequest
{
    [Required, MaxLength(16)]
    public string Symbol { get; set; } = string.Empty;

    [Range(0.00000001, double.MaxValue, ErrorMessage = "Quantity must be greater than 0.")]
    public decimal Quantity { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
