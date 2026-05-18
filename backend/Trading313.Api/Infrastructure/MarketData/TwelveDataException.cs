namespace Trading313.Api.Infrastructure.MarketData;

public class TwelveDataException : Exception
{
    public int? Code { get; }
    public string? Endpoint { get; }

    public TwelveDataException(string message, int? code = null, string? endpoint = null, Exception? inner = null)
        : base(message, inner)
    {
        Code = code;
        Endpoint = endpoint;
    }
}

public class TwelveDataRateLimitException : TwelveDataException
{
    public TwelveDataRateLimitException(string message, string? endpoint = null)
        : base(message, code: 429, endpoint: endpoint)
    {
    }
}
