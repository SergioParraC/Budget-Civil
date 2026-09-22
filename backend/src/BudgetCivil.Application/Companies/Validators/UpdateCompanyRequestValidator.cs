using BudgetCivil.Application.Companies.Dtos;
using FluentValidation;

namespace BudgetCivil.Application.Companies.Validators;

public sealed class UpdateCompanyRequestValidator : AbstractValidator<UpdateCompanyRequest>
{
    public UpdateCompanyRequestValidator()
    {
        RuleFor(request => request.BusinessName)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(request => request.TaxId)
            .NotEmpty()
            .MaximumLength(20);
    }
}
