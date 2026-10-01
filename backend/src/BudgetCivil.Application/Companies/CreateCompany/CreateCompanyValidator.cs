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
            .Must(companyType =>
                string.IsNullOrWhiteSpace(companyType) ||
                string.Equals(companyType, "PUBLICA", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(companyType, "PRIVADA", StringComparison.OrdinalIgnoreCase))
            .WithMessage("company_type must be PUBLICA or PRIVADA.");

        RuleFor(command => command.TerritorialLevel)
            .NotEmpty()
            .When(command => string.Equals(command.CompanyType, "PUBLICA", StringComparison.OrdinalIgnoreCase))
            .WithMessage("territorial_level is required for PUBLICA company_type.");

        When(command => command.Representative is not null, () =>
        {
            RuleFor(command => command.Representative!.Mode)
                .NotEmpty();

            RuleFor(command => command.Representative!.Mode)
                .Must(mode =>
                    string.Equals(mode, "USER_REFERENCE", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(mode, "MANUAL_DATA", StringComparison.OrdinalIgnoreCase))
                .When(command => !string.IsNullOrWhiteSpace(command.Representative!.Mode));

            RuleFor(command => command.Representative!.UserId)
                .NotNull()
                .When(command => string.Equals(command.Representative!.Mode, "USER_REFERENCE", StringComparison.OrdinalIgnoreCase));

            RuleFor(command => command.Representative!.FullName)
                .NotEmpty()
                .When(command => string.Equals(command.Representative!.Mode, "MANUAL_DATA", StringComparison.OrdinalIgnoreCase));

            RuleFor(command => command.Representative!.DocumentNumber)
                .NotEmpty()
                .When(command => string.Equals(command.Representative!.Mode, "MANUAL_DATA", StringComparison.OrdinalIgnoreCase));
        });
    }
}
