using BudgetCivil.Domain.Resources;

namespace BudgetCivil.Application.Resources.Repositories;

public sealed record ResourceFilter(
    Guid ProjectId,
    string? Search,
    Guid? ResourceTypeId,
    QuotationStatus? QuotationStatus,
    string? Brand,
    Guid? MunicipalityId,
    decimal? MinUnitPrice,
    decimal? MaxUnitPrice);

public enum ResourceSortField
{
    Description = 1,
    UnitPrice = 2,
    QuotationStatus = 3,
    LastModifiedAt = 4
}

public sealed record ResourceSort(ResourceSortField Field, bool Descending);

public interface IResourceRepository
{
    Task<Resource?> GetByIdAsync(Guid projectId, Guid resourceId, CancellationToken cancellationToken = default);

    Task<Resource?> GetByIdAsync(Guid resourceId, CancellationToken cancellationToken = default);

    Task<bool> ResourceTypeExistsAsync(Guid resourceTypeId, CancellationToken cancellationToken = default);

    Task<bool> MunicipalityExistsAsync(Guid municipalityId, CancellationToken cancellationToken = default);

    Task AddAsync(Resource resource, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Resource>> ListAsync(
        ResourceFilter filter,
        ResourceSort sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(ResourceFilter filter, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Resource>> GetSimilarAsync(
        Guid projectId,
        string? description,
        string? userKey,
        int limit,
        CancellationToken cancellationToken = default);
}
