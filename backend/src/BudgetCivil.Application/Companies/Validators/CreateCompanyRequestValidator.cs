using BudgetCivil.Application.Companies.Dtos;
using FluentValidation;

namespace BudgetCivil.Application.Companies.Validators;

public sealed class CreateCompanyRequestValidator : AbstractValidator<CreateCompanyRequest>
{
    public CreateCompanyRequestValidator()
    {
        RuleFor(request => request.BusinessName)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(request => request.TaxId)
            .NotEmpty()
            .MaximumLength(20);
    }
}
