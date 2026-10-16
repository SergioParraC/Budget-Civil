using BudgetCivil.Application.Profile.Dtos;
using FluentValidation;

namespace BudgetCivil.Application.Profile.Validators;

public sealed class ChangeMyPasswordRequestValidator : AbstractValidator<ChangeMyPasswordRequest>
{
    public ChangeMyPasswordRequestValidator()
    {
        RuleFor(request => request.CurrentPassword)
            .NotEmpty();

        RuleFor(request => request.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must include at least one uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must include at least one number.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must include at least one special character.");
    }
}
