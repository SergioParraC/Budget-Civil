using FluentValidation;

namespace BudgetCivil.Application.Common.Pagination;

public sealed class PaginationValidator : AbstractValidator<PaginationQuery>
{
    public PaginationValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(query => query.PageSize)
            .Must(PaginationQuery.IsAllowedPageSize)
            .WithMessage("pageSize must be one of: 10, 50 or 100.")
            .WithErrorCode("PAGE_SIZE_INVALID");
    }
}
