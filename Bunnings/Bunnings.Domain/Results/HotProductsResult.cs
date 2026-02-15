namespace Bunnings.Domain.Results;

public record HotProductsResult(IReadOnlyList<DailyTopProduct> DailyTop, PeriodTopProduct TopLast3Days);

public record DailyTopProduct(DateOnly Date, string ProductName);

public record PeriodTopProduct(DateOnly From, DateOnly To, string ProductName);