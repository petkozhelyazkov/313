using System.ComponentModel.DataAnnotations;
using Trading313.Api.Domain.Entities;

namespace Trading313.Api.Dtos.Alerts;

public class CreateAlertRequest
{
    [Required, MaxLength(16)]
    public string Symbol { get; set; } = string.Empty;

    [Required]
    public AlertDirection Direction { get; set; }

    [Range(0.0001, double.MaxValue, ErrorMessage = "Trigger price must be > 0.")]
    public decimal TriggerPrice { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public record PriceAlertDto(
    long Id,
    string Symbol,
    string? Name,
    string? LogoUrl,
    string Direction,
    decimal TriggerPrice,
    string Status,
    decimal? CurrentPrice,
    bool Acknowledged,
    DateTime CreatedAt,
    DateTime? TriggeredAt,
    decimal? TriggeredPrice,
    string? Notes);
