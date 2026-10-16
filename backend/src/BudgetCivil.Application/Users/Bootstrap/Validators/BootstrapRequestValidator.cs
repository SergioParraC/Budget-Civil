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

        RuleFor(request => request.AdminProfile.CountryId).NotEmpty();
        RuleFor(request => request.AdminProfile.DepartmentId).NotEmpty();
        RuleFor(request => request.AdminProfile.MunicipalityId).NotEmpty();

        RuleFor(request => request.Company.BusinessName)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(request => request.Company.TaxId)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(request => request.Company.CompanyType)
            .NotEmpty()
            .Must(companyType =>
                string.Equals(companyType?.Trim(), "PUBLICA", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(companyType?.Trim(), "PRIVADA", StringComparison.OrdinalIgnoreCase));

        RuleFor(request => request.Company.TerritorialLevel)
            .NotEmpty()
            .When(request => string.Equals(request.Company.CompanyType?.Trim(), "PUBLICA", StringComparison.OrdinalIgnoreCase));

        RuleFor(request => request.Company.CountryId).NotEmpty();
        RuleFor(request => request.Company.DepartmentId).NotEmpty();
        RuleFor(request => request.Company.MunicipalityId).NotEmpty();

        RuleFor(request => request.Company.Representative)
            .NotNull();

        RuleFor(request => request.Company.Representative!.Mode)
            .Must(mode =>
                string.Equals(mode?.Trim(), "USER_REFERENCE", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(mode?.Trim(), "MANUAL_DATA", StringComparison.OrdinalIgnoreCase))
            .When(request => request.Company.Representative is not null);

        RuleFor(request => request.Company.Representative!.UserId)
            .NotNull()
            .When(request => string.Equals(request.Company.Representative?.Mode?.Trim(), "USER_REFERENCE", StringComparison.OrdinalIgnoreCase));

        RuleFor(request => request.Company.Representative!.FullName)
            .NotEmpty()
            .MaximumLength(200)
            .When(request => string.Equals(request.Company.Representative?.Mode?.Trim(), "MANUAL_DATA", StringComparison.OrdinalIgnoreCase));

        RuleFor(request => request.Company.Representative!.DocumentNumber)
            .NotEmpty()
            .MaximumLength(50)
            .When(request => string.Equals(request.Company.Representative?.Mode?.Trim(), "MANUAL_DATA", StringComparison.OrdinalIgnoreCase));
    }
}
