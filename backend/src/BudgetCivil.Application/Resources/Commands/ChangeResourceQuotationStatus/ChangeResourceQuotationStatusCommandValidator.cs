using BudgetCivil.Application.Resources.Repositories;
using BudgetCivil.Domain.Resources;
using FluentValidation;

namespace BudgetCivil.Application.Resources.Commands.ChangeResourceQuotationStatus;

/// <summary>
/// `scope` es obligatorio y exclusivo: `resourceIds` solo con `SELECTION` y `filter` solo con `FILTER`
/// (`D-11`, `SC-COTIZACION-04`).
/// </summary>
public sealed class ChangeResourceQuotationStatusCommandValidator
    : AbstractValidator<ChangeResourceQuotationStatusCommand>
{
    public ChangeResourceQuotationStatusCommandValidator()
    {
        RuleFor(command => command.Scope)
            .NotEmpty()
            .Must(scope => ChangeResourceQuotationStatusCommand.TryParseScope(scope, out _))
            .WithMessage("scope is invalid.");

        RuleFor(command => command.ResourceIds)
            .Must(ids => ids is { Count: > 0 } && ids.All(id => id != Guid.Empty))
            .WithMessage("resourceIds must contain at least one valid identifier.")
            .When(command => IsScope(command, ResourceQuotationScope.Selection));

        RuleFor(command => command.ResourceIds)
            .Null()
            .WithMessage("resourceIds is only allowed for SELECTION.")
            .When(command => IsScope(command, ResourceQuotationScope.Filter) ||
                IsScope(command, ResourceQuotationScope.All));

        RuleFor(command => command.Filter)
            .NotNull()
            .WithMessage("filter is required for FILTER.")
            .When(command => IsScope(command, ResourceQuotationScope.Filter));

        RuleFor(command => command.Filter)
            .Null()
            .WithMessage("filter is only allowed for FILTER.")
            .When(command => IsScope(command, ResourceQuotationScope.Selection) ||
                IsScope(command, ResourceQuotationScope.All));

        When(command => command.Filter is not null, () =>
        {
            RuleFor(command => command.Filter!.Description)
                .MaximumLength(Resource.DescriptionMaxLength)
                .When(command => !string.IsNullOrWhiteSpace(command.Filter!.Description));

            RuleFor(command => command.Filter!.Brand)
                .MaximumLength(Resource.BrandMaxLength)
                .When(command => !string.IsNullOrWhiteSpace(command.Filter!.Brand));

            RuleFor(command => command.Filter!.ResourceTypeId)
                .NotEqual(Guid.Empty)
                .When(command => command.Filter!.ResourceTypeId.HasValue);

            RuleFor(command => command.Filter!.MunicipalityId)
                .NotEqual(Guid.Empty)
                .When(command => command.Filter!.MunicipalityId.HasValue);

            RuleFor(command => command.Filter!.QuotationStatus)
                .Must(status => !string.IsNullOrWhiteSpace(status) &&
                    QuotationStatusCodes.TryParse(status!, out _))
                .WithMessage("quotationStatus is invalid.")
                .When(command => !string.IsNullOrWhiteSpace(command.Filter!.QuotationStatus));

            RuleFor(command => command.Filter!.MinPrice)
                .GreaterThanOrEqualTo(0m)
                .When(command => command.Filter!.MinPrice.HasValue);

            RuleFor(command => command.Filter!.MaxPrice)
                .GreaterThanOrEqualTo(0m)
                .When(command => command.Filter!.MaxPrice.HasValue);

            RuleFor(command => command.Filter)
                .Must(filter => !filter!.MinPrice.HasValue ||
                    !filter.MaxPrice.HasValue ||
                    filter.MinPrice.Value <= filter.MaxPrice.Value)
                .WithMessage("Unit price range is invalid.");
        });
    }

    private static bool IsScope(ChangeResourceQuotationStatusCommand command, ResourceQuotationScope scope)
    {
        return ChangeResourceQuotationStatusCommand.TryParseScope(command.Scope, out var parsed) &&
            parsed == scope;
    }
}
