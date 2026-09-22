using BudgetCivil.Application.Users.Administration.Dtos;
using FluentValidation;

namespace BudgetCivil.Application.Users.Administration.Validators;

public sealed class ChangeUserRoleRequestValidator : AbstractValidator<ChangeUserRoleRequest>
{
    public ChangeUserRoleRequestValidator()
    {
        RuleFor(request => request.AssignedRoleCode)
            .NotEmpty()
            .MaximumLength(50);
    }
}
