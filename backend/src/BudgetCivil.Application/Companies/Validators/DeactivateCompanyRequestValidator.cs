using BudgetCivil.Application.Companies.Dtos;
using FluentValidation;

namespace BudgetCivil.Application.Companies.Validators;

public sealed class DeactivateCompanyRequestValidator : AbstractValidator<DeactivateCompanyRequest>
{
    public DeactivateCompanyRequestValidator()
    {
        RuleFor(request => request.Reason)
            .NotEmpty()
            .MaximumLength(255);
    }
}
