namespace BudgetCivil.Application.Common.Pagination;

public sealed class PaginationQuery
{
    public static readonly int[] AllowedPageSizes = [10, 50, 100];

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 10;

    public static bool IsAllowedPageSize(int pageSize)
    {
        return AllowedPageSizes.Contains(pageSize);
    }
}
