using BudgetCivil.Application.Users.Bootstrap.Dtos;
using FluentValidation;

namespace BudgetCivil.Application.Users.Bootstrap.Validators;

public sealed class BootstrapRequestValidator : AbstractValidator<BootstrapRequest>
{
    public BootstrapRequestValidator()
    {
        RuleFor(request => request.AdminProfile)
            .NotNull();

        RuleFor(request => request.Company)
            .NotNull();

        RuleFor(request => request.AdminProfile.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(request => request.AdminProfile.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(request => request.AdminProfile.DocumentType)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(request => request.AdminProfile.DocumentNumber)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(request => request.AdminProfile.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(150);

        RuleFor(request => request.AdminProfile.Username)
            .NotEmpty()
            .MinimumLength(4)
            .MaximumLength(100);

        RuleFor(request => request.AdminProfile.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must include at least one uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must include at least one number.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must include at least one special character.");

        RuleFor(request => request.Company.BusinessName)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(request => request.Company.TaxId)
            .NotEmpty()
            .MaximumLength(20);
    }
}
