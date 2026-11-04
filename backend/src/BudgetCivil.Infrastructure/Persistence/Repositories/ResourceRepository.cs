using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Resources.Repositories;
using BudgetCivil.Domain.Resources;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Infrastructure.Persistence.Repositories;

public sealed class ResourceRepository(BudgetCivilDbContext dbContext) : IResourceRepository
{
    private BudgetCivilDbContext DbContext { get; } = dbContext;
    public Task<Resource?> GetByIdAsync(
        Guid projectId,
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        return DbContext.Resources
            .Include(resource => resource.ResourceType)
            .SingleOrDefaultAsync(
                resource => resource.ProjectId == projectId && resource.Id == resourceId,
                cancellationToken);
    }

    public Task<Resource?> GetByIdAsync(Guid resourceId, CancellationToken cancellationToken = default)
    {
        return DbContext.Resources
            .Include(resource => resource.ResourceType)
            .SingleOrDefaultAsync(resource => resource.Id == resourceId, cancellationToken);
    }

    public Task<bool> ResourceTypeExistsAsync(
        Guid resourceTypeId,
        CancellationToken cancellationToken = default)
    {
        return DbContext.ResourceTypes.AnyAsync(
            resourceType => resourceType.Id == resourceTypeId && resourceType.IsActive,
            cancellationToken);
    }

    public Task<bool> MunicipalityExistsAsync(
        Guid municipalityId,
        CancellationToken cancellationToken = default)
    {
        return DbContext.Municipalities.AnyAsync(
            municipality => municipality.Id == municipalityId && municipality.IsActive,
            cancellationToken);
    }

    public Task AddAsync(Resource resource, CancellationToken cancellationToken = default)
    {
        DbContext.Resources.Add(resource);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await DbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictAppException(
                "Resource was modified by another user.",
                "RESOURCE_CONCURRENCY_CONFLICT");
        }
    }

    public async Task<IReadOnlyList<Resource>> ListAsync(
        ResourceFilter filter,
        ResourceSort sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = ApplySort(BuildFilterQuery(filter), sort);

        return await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(ResourceFilter filter, CancellationToken cancellationToken = default)
    {
        return BuildFilterQuery(filter).CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Resource>> GetSimilarAsync(
        Guid projectId,
        string? description,
        string? userKey,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var query = DbContext.Resources
            .AsNoTracking()
            .Include(resource => resource.ResourceType)
            .Where(resource => resource.ProjectId == projectId);

        var normalizedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        var normalizedUserKey = string.IsNullOrWhiteSpace(userKey) ? null : userKey.Trim();

        if (normalizedDescription is not null && normalizedUserKey is not null)
        {
            query = query.Where(resource =>
                resource.Description.Contains(normalizedDescription) ||
                resource.UserKey == normalizedUserKey);
        }
        else if (normalizedDescription is not null)
        {
            query = query.Where(resource => resource.Description.Contains(normalizedDescription));
        }
        else if (normalizedUserKey is not null)
        {
            query = query.Where(resource => resource.UserKey == normalizedUserKey);
        }
        else
        {
            return [];
        }

        return await query
            .OrderBy(resource => resource.Description)
            .ThenBy(resource => resource.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    private IQueryable<Resource> BuildFilterQuery(ResourceFilter filter)
    {
        var query = DbContext.Resources
            .AsNoTracking()
            .Include(resource => resource.ResourceType)
            .Where(resource => resource.ProjectId == filter.ProjectId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(resource => resource.Description.Contains(search));
        }

        if (filter.ResourceTypeId.HasValue)
        {
            query = query.Where(resource => resource.ResourceTypeId == filter.ResourceTypeId.Value);
        }

        if (filter.QuotationStatus.HasValue)
        {
            query = query.Where(resource => resource.QuotationStatus == filter.QuotationStatus.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Brand))
        {
            var brand = filter.Brand.Trim();
            query = query.Where(resource => resource.Brand == brand);
        }

        if (filter.MunicipalityId.HasValue)
        {
            query = query.Where(resource => resource.MunicipalityId == filter.MunicipalityId.Value);
        }

        if (filter.MinUnitPrice.HasValue)
        {
            query = query.Where(resource => resource.UnitPrice >= filter.MinUnitPrice.Value);
        }

        if (filter.MaxUnitPrice.HasValue)
        {
            query = query.Where(resource => resource.UnitPrice <= filter.MaxUnitPrice.Value);
        }

        return query;
    }

    private static IQueryable<Resource> ApplySort(IQueryable<Resource> query, ResourceSort sort)
    {
        var ordered = sort.Field switch
        {
            ResourceSortField.UnitPrice => sort.Descending
                ? query.OrderByDescending(resource => resource.UnitPrice)
                : query.OrderBy(resource => resource.UnitPrice),
            ResourceSortField.QuotationStatus => sort.Descending
                ? query.OrderByDescending(resource => resource.QuotationStatus)
                : query.OrderBy(resource => resource.QuotationStatus),
            ResourceSortField.LastModifiedAt => sort.Descending
                ? query.OrderByDescending(resource => resource.LastModifiedAtUtc)
                : query.OrderBy(resource => resource.LastModifiedAtUtc),
            _ => sort.Descending
                ? query.OrderByDescending(resource => resource.Description)
                : query.OrderBy(resource => resource.Description)
        };

        return ordered.ThenBy(resource => resource.Id);
    }
}
