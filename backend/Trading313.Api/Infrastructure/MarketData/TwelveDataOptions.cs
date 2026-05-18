namespace Trading313.Api.Infrastructure.MarketData;

public class TwelveDataOptions
{
    public const string SectionName = "TwelveData";

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.twelvedata.com";
    public int RequestsPerMinute { get; set; } = 8;
    public int RequestsPerDay { get; set; } = 800;
}
