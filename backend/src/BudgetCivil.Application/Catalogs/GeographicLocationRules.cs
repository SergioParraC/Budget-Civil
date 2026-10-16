using BudgetCivil.Application.Catalogs.Repositories;
using BudgetCivil.Application.Common.Exceptions;

namespace BudgetCivil.Application.Catalogs;

public static class GeographicLocationRules
{
    public static async Task EnsureValidActiveLocationAsync(
        IGeographicCatalogRepository repository,
        Guid countryId,
        Guid departmentId,
        Guid municipalityId,
        CancellationToken cancellationToken = default)
    {
        if (countryId == Guid.Empty ||
            departmentId == Guid.Empty ||
            municipalityId == Guid.Empty ||
            !await repository.IsValidActiveLocationAsync(countryId, departmentId, municipalityId, cancellationToken))
        {
            throw new ValidationAppException(
                "Country, department and municipality must be active catalog entries in the same hierarchy.",
                "VALIDATION_ERROR");
        }
    }
}
