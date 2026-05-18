namespace Trading313.Api.Domain.Entities;

/// <summary>
/// One row per outbound Twelve Data call. Powers both rate-limit persistence
/// across restarts and the admin "API usage" panel.
/// </summary>
public class ApiUsageLogEntry
{
    public long Id { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string? Symbols { get; set; }
    public DateTime RequestedAt { get; set; }
    public int StatusCode { get; set; }
    public long ResponseTimeMs { get; set; }
    public int QuotaUsedToday { get; set; }
}
