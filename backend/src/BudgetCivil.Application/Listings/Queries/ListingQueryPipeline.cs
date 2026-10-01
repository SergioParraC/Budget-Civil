using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Common.Pagination;
using FluentValidation;

namespace BudgetCivil.Application.Listings.Queries;

public sealed record ListingPagination(int PageNumber, int PageSize);

public sealed class ListingQueryPipeline(IValidator<PaginationQuery> paginationValidator)
{
    public async Task<ListingPagination> ValidateAndNormalizeAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var paginationQuery = new PaginationQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var validationResult = await paginationValidator.ValidateAsync(paginationQuery, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationAppException("Invalid pagination query.", "PAGE_SIZE_INVALID");
        }

        return new ListingPagination(paginationQuery.PageNumber, paginationQuery.PageSize);
    }
}
