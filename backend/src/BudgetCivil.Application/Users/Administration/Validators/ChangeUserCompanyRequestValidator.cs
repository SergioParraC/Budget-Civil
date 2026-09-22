using BudgetCivil.Application.Users.Administration.Dtos;
using FluentValidation;

namespace BudgetCivil.Application.Users.Administration.Validators;

public sealed class ChangeUserCompanyRequestValidator : AbstractValidator<ChangeUserCompanyRequest>
{
    public ChangeUserCompanyRequestValidator()
    {
        RuleFor(request => request.CompanyId)
            .NotEmpty();
    }
}
