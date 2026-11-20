using BudgetCivil.Application.Projects.Repositories;
using BudgetCivil.Application.Resources.Repositories;
using BudgetCivil.Domain.Resources;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Infrastructure.Persistence.Repositories;

/// <summary>
/// Resolución set-based, conteo y `UPDATE` del cambio masivo de cotización (`D-12`, `REQ-COTIZACION-01`,
/// `REQ-COTIZACION-05`). También implementa el conteo que exige `REQ-PROYECTOS-05` (`D-13`).
/// </summary>
public sealed class ResourceQuotationRepository(BudgetCivilDbContext dbContext) :
    IResourceQuotationRepository,
    IProjectQuotationStatusReader
{
    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await operation(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ResourceQuotationCandidate>> GetByIdsAsync(
        Guid projectId,
        IReadOnlyCollection<Guid> resourceIds,
        CancellationToken cancellationToken = default)
    {
        if (resourceIds.Count == 0)
        {
            return [];
        }

        var resources = await ProjectResources(projectId)
            .Where(resource => resourceIds.Contains(resource.Id))
            .ToListAsync(cancellationToken);

        return await ToCandidatesAsync(resources, cancellationToken);
    }

    public async Task<IReadOnlyList<ResourceQuotationCandidate>> GetByFilterAsync(
        Guid projectId,
        ResourceQuotationFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = ProjectResources(projectId);

        if (!string.IsNullOrWhiteSpace(filter.Description))
        {
            var description = filter.Description.Trim();
            query = query.Where(resource => resource.Description.Contains(description));
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

        if (filter.MinPrice.HasValue)
        {
            query = query.Where(resource => resource.UnitPrice >= filter.MinPrice.Value);
        }

        if (filter.MaxPrice.HasValue)
        {
            query = query.Where(resource => resource.UnitPrice <= filter.MaxPrice.Value);
        }

        var resources = await query.ToListAsync(cancellationToken);
        return await ToCandidatesAsync(resources, cancellationToken);
    }

    public async Task<IReadOnlyList<ResourceQuotationCandidate>> GetAllAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var resources = await ProjectResources(projectId).ToListAsync(cancellationToken);
        return await ToCandidatesAsync(resources, cancellationToken);
    }

    public async Task<int> MarkQuotedAsync(
        Guid projectId,
        IReadOnlyCollection<Guid> resourceIds,
        Guid actorUserId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        if (resourceIds.Count == 0)
        {
            return 0;
        }

        var resources = await dbContext.Resources
            .Where(resource => resource.ProjectId == projectId && resourceIds.Contains(resource.Id))
            .ToListAsync(cancellationToken);

        var updated = 0;
        foreach (var resource in resources)
        {
            if (resource.QuotationStatus == QuotationStatus.Cotizado)
            {
                continue;
            }

            resource.QuotationStatus = QuotationStatus.Cotizado;
            resource.LastModifiedByUserId = actorUserId;
            resource.LastModifiedAtUtc = utcNow;
            updated++;
        }

        if (updated > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return updated;
    }

    public Task<int> CountPendingAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Resources
            .AsNoTracking()
            .CountAsync(
                resource =>
                    resource.ProjectId == projectId &&
                    resource.QuotationStatus != QuotationStatus.Cotizado,
                cancellationToken);
    }

    /// <summary>Conteo del proyecto para la precondición de `REQ-PROYECTOS-05` (`D-13`).</summary>
    public Task<int> CountPendingResourcesAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        return CountPendingAsync(projectId, cancellationToken);
    }

    private IQueryable<Resource> ProjectResources(Guid projectId)
    {
        return dbContext.Resources
            .AsNoTracking()
            .Include(resource => resource.ResourceType)
            .Where(resource => resource.ProjectId == projectId);
    }

    private async Task<IReadOnlyList<ResourceQuotationCandidate>> ToCandidatesAsync(
        IReadOnlyList<Resource> resources,
        CancellationToken cancellationToken)
    {
        var matrixIds = resources
            .Where(resource => IsMatrix(resource.ResourceType.Code))
            .Select(resource => resource.Id)
            .ToList();

        var componentCounts = matrixIds.Count == 0
            ? new Dictionary<Guid, int>()
            : await dbContext.MatrixComponents
                .AsNoTracking()
                .Where(component => matrixIds.Contains(component.MatrixResourceId))
                .GroupBy(component => component.MatrixResourceId)
                .Select(group => new { MatrixResourceId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(row => row.MatrixResourceId, row => row.Count, cancellationToken);

        return resources
            .Select(resource => new ResourceQuotationCandidate(
                resource.Id,
                resource.Description,
                resource.Unit,
                resource.UnitPrice,
                resource.Brand,
                resource.SupplierId,
                resource.QuotationStatus,
                resource.IsComplete,
                IsMatrix(resource.ResourceType.Code),
                componentCounts.GetValueOrDefault(resource.Id)))
            .ToList();
    }

    private static bool IsMatrix(string? resourceTypeCode)
    {
        return string.Equals(resourceTypeCode, ResourceTypeCatalog.Matrix, StringComparison.OrdinalIgnoreCase);
    }
}
