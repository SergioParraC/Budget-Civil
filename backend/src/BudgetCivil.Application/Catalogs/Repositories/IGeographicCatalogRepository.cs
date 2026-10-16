namespace BudgetCivil.Application.Catalogs.Repositories;

public interface IGeographicCatalogRepository
{
    Task<bool> IsValidActiveLocationAsync(
        Guid countryId,
        Guid departmentId,
        Guid municipalityId,
        CancellationToken cancellationToken = default);

    Task<bool> IsActiveDocumentTypeAsync(Guid documentTypeId, CancellationToken cancellationToken = default);

    Task<bool> IsActiveUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
