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

        RuleFor(request => request.CompanyType)
            .NotEmpty()
            .Must(companyType =>
                string.Equals(companyType?.Trim(), "PUBLICA", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(companyType?.Trim(), "PRIVADA", StringComparison.OrdinalIgnoreCase));

        RuleFor(request => request.TerritorialLevel)
            .NotEmpty()
            .When(request => string.Equals(request.CompanyType?.Trim(), "PUBLICA", StringComparison.OrdinalIgnoreCase));

        RuleFor(request => request.CountryId).NotEmpty();
        RuleFor(request => request.DepartmentId).NotEmpty();
        RuleFor(request => request.MunicipalityId).NotEmpty();

        RuleFor(request => request.Representative)
            .NotNull();

        RuleFor(request => request.Representative!.Mode)
            .Must(mode =>
                string.Equals(mode, "USER_REFERENCE", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(mode, "MANUAL_DATA", StringComparison.OrdinalIgnoreCase))
            .When(request => request.Representative is not null);

        RuleFor(request => request.Representative!.UserId)
            .NotNull()
            .When(request => string.Equals(request.Representative?.Mode, "USER_REFERENCE", StringComparison.OrdinalIgnoreCase));

        RuleFor(request => request.Representative!.FullName)
            .NotEmpty()
            .MaximumLength(200)
            .When(request => string.Equals(request.Representative?.Mode, "MANUAL_DATA", StringComparison.OrdinalIgnoreCase));

        RuleFor(request => request.Representative!.DocumentNumber)
            .NotEmpty()
            .MaximumLength(50)
            .When(request => string.Equals(request.Representative?.Mode, "MANUAL_DATA", StringComparison.OrdinalIgnoreCase));
    }
}
