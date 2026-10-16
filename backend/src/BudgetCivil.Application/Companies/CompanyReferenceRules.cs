using BudgetCivil.Application.Catalogs;
using BudgetCivil.Application.Catalogs.Repositories;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Companies.Dtos;

namespace BudgetCivil.Application.Companies;

public static class CompanyReferenceRules
{
    public static async Task EnsureValidReferencesAsync(
        IGeographicCatalogRepository repository,
        Guid countryId,
        Guid departmentId,
        Guid municipalityId,
        LegalRepresentativePayload? representative,
        CancellationToken cancellationToken = default)
    {
        await GeographicLocationRules.EnsureValidActiveLocationAsync(
            repository,
            countryId,
            departmentId,
            municipalityId,
            cancellationToken);

        if (representative is null)
        {
            throw new ValidationAppException("Representative data is required.", "VALIDATION_ERROR");
        }

        var mode = representative.Mode?.Trim();
        if (string.Equals(mode, "USER_REFERENCE", StringComparison.OrdinalIgnoreCase))
        {
            if (!representative.UserId.HasValue ||
                !await repository.IsActiveUserAsync(representative.UserId.Value, cancellationToken))
            {
                throw new ValidationAppException("Representative user does not exist or is inactive.", "VALIDATION_ERROR");
            }

            if (!string.IsNullOrWhiteSpace(representative.FullName) ||
                representative.DocumentTypeId.HasValue ||
                !string.IsNullOrWhiteSpace(representative.DocumentNumber))
            {
                throw new ValidationAppException("Manual representative fields are not allowed for USER_REFERENCE mode.", "VALIDATION_ERROR");
            }
        }
        else if (string.Equals(mode, "MANUAL_DATA", StringComparison.OrdinalIgnoreCase) &&
                 representative.UserId.HasValue)
        {
            throw new ValidationAppException("Representative user ID is not allowed for MANUAL_DATA mode.", "VALIDATION_ERROR");
        }

        if (representative.DocumentTypeId.HasValue &&
            !await repository.IsActiveDocumentTypeAsync(representative.DocumentTypeId.Value, cancellationToken))
        {
            throw new ValidationAppException("Representative document type does not exist or is inactive.", "VALIDATION_ERROR");
        }
    }
}
