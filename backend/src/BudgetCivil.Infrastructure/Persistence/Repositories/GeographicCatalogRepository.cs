using BudgetCivil.Application.Catalogs.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Infrastructure.Persistence.Repositories;

public sealed class GeographicCatalogRepository(BudgetCivilDbContext dbContext) : IGeographicCatalogRepository
{
    public Task<bool> IsValidActiveLocationAsync(
        Guid countryId,
        Guid departmentId,
        Guid municipalityId,
        CancellationToken cancellationToken = default)
    {
        return (
            from municipality in dbContext.Municipalities
            join department in dbContext.Departments on municipality.DepartmentId equals department.Id
            join country in dbContext.Countries on department.CountryId equals country.Id
            where municipality.Id == municipalityId
                && municipality.IsActive
                && department.Id == departmentId
                && department.IsActive
                && country.Id == countryId
                && country.IsActive
            select municipality.Id)
            .AnyAsync(cancellationToken);
    }

    public Task<bool> IsActiveDocumentTypeAsync(Guid documentTypeId, CancellationToken cancellationToken = default)
    {
        return dbContext.DocumentTypes
            .AnyAsync(documentType => documentType.Id == documentTypeId && documentType.IsActive, cancellationToken);
    }

    public Task<bool> IsActiveUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return dbContext.UserAccounts
            .AnyAsync(user => user.Id == userId && user.AccountEnabled, cancellationToken);
    }
}
