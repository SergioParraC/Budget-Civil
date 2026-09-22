using BudgetCivil.Application.Profile.Dtos;
using FluentValidation;

namespace BudgetCivil.Application.Profile.Validators;

public sealed class EnableUserPasswordRecoveryRequestValidator : AbstractValidator<EnableUserPasswordRecoveryRequest>
{
    public EnableUserPasswordRecoveryRequestValidator()
    {
        RuleFor(request => request.Reason)
            .NotEmpty()
            .MaximumLength(255);
    }
}
