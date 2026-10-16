using FluentValidation;

namespace BudgetCivil.Application.Companies.CreateCompany;

public sealed class CreateCompanyValidator : AbstractValidator<CreateCompanyCommand>
{
    public CreateCompanyValidator()
    {
        RuleFor(command => command.BusinessName)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(command => command.TaxId)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(command => command.CompanyType)
            .NotEmpty()
            .Must(companyType =>
                string.Equals(companyType?.Trim(), "PUBLICA", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(companyType?.Trim(), "PRIVADA", StringComparison.OrdinalIgnoreCase))
            .WithMessage("company_type must be PUBLICA or PRIVADA.");

        RuleFor(command => command.CountryId).NotEmpty();
        RuleFor(command => command.DepartmentId).NotEmpty();
        RuleFor(command => command.MunicipalityId).NotEmpty();

        RuleFor(command => command.TerritorialLevel)
            .NotEmpty()
            .When(command => string.Equals(command.CompanyType?.Trim(), "PUBLICA", StringComparison.OrdinalIgnoreCase))
            .WithMessage("territorial_level is required for PUBLICA company_type.");

        RuleFor(command => command.Representative)
            .NotNull();

        When(command => command.Representative is not null, () =>
        {
            RuleFor(command => command.Representative!.Mode)
                .NotEmpty();

            RuleFor(command => command.Representative!.Mode)
                .Must(mode =>
                    string.Equals(mode?.Trim(), "USER_REFERENCE", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(mode?.Trim(), "MANUAL_DATA", StringComparison.OrdinalIgnoreCase))
                .When(command => !string.IsNullOrWhiteSpace(command.Representative!.Mode));

            RuleFor(command => command.Representative!.UserId)
                .NotNull()
                .When(command => string.Equals(command.Representative!.Mode?.Trim(), "USER_REFERENCE", StringComparison.OrdinalIgnoreCase));

            RuleFor(command => command.Representative!.FullName)
                .NotEmpty()
                .When(command => string.Equals(command.Representative!.Mode?.Trim(), "MANUAL_DATA", StringComparison.OrdinalIgnoreCase));

            RuleFor(command => command.Representative!.DocumentNumber)
                .NotEmpty()
                .When(command => string.Equals(command.Representative!.Mode?.Trim(), "MANUAL_DATA", StringComparison.OrdinalIgnoreCase));

            RuleFor(command => command.Representative!.FullName)
                .MaximumLength(200)
                .When(command => !string.IsNullOrWhiteSpace(command.Representative!.FullName));

            RuleFor(command => command.Representative!.DocumentNumber)
                .MaximumLength(50)
                .When(command => !string.IsNullOrWhiteSpace(command.Representative!.DocumentNumber));
        });
    }
}
