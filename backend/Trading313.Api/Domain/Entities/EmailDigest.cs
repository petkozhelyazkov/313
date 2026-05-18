namespace Trading313.Api.Domain.Entities;

/// <summary>
/// A periodic per-user activity summary. Even when SMTP is disabled we keep the
/// digest in-app so the user can read it from the Profile page.
/// </summary>
public class EmailDigest
{
    public long Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public string BodyText { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
}
