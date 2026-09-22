using BudgetCivil.Application.Users.Administration.Dtos;
using FluentValidation;

namespace BudgetCivil.Application.Users.Administration.Validators;

public sealed class DeactivateUserRequestValidator : AbstractValidator<DeactivateUserRequest>
{
    public DeactivateUserRequestValidator()
    {
        RuleFor(request => request.Reason)
            .NotEmpty()
            .MaximumLength(255);
    }
}
