namespace Trading313.Api.Dtos.Portfolio;

public record TransactionListResponse(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<TransactionDto> Items);

public record TagPlSummary(string Tag, decimal RealizedPl, int TransactionCount);
