using BudgetCivil.Application.Dtos;
using FluentValidation;

namespace BudgetCivil.Application.Validators;

public sealed class EnableUserPasswordRecoveryRequestValidator : AbstractValidator<EnableUserPasswordRecoveryRequest>
{
    public EnableUserPasswordRecoveryRequestValidator()
    {
        RuleFor(request => request.Reason)
            .NotEmpty()
            .MaximumLength(255);
    }
}
