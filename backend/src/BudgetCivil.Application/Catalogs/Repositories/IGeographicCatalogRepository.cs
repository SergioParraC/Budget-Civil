using BudgetCivil.Domain.Catalogs;

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

    Task<(IReadOnlyList<Country> Items, int Total)> GetCountriesAsync(
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Department> Items, int Total)> GetDepartmentsAsync(
        Guid? countryId,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Municipality> Items, int Total)> GetMunicipalitiesAsync(
        Guid? departmentId,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<BaseCurrency> Items, int Total)> GetBaseCurrenciesAsync(
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<DocumentType> Items, int Total)> GetDocumentTypesAsync(
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
}
