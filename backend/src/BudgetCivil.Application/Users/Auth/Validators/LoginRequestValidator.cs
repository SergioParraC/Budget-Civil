using BudgetCivil.Application.Users.Auth.Dtos;
using FluentValidation;

namespace BudgetCivil.Application.Users.Auth.Validators;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Identifier)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(request => request.Password)
            .NotEmpty()
            .MaximumLength(256);
    }
}
