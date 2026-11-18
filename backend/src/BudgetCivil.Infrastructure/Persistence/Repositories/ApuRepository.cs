using BudgetCivil.Application.Apus.Repositories;
using BudgetCivil.Domain.Apus;
using BudgetCivil.Domain.Resources;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Infrastructure.Persistence.Repositories;

/// <summary>
/// Persistencia de `M-05` sobre el `DbContext` único (`D-54`, `D-95`): APU, composición, Matrices, costo,
/// conteo de uso y transacciones.
/// </summary>
public sealed class ApuRepository(BudgetCivilDbContext dbContext) : IApuRepository
{
    private BudgetCivilDbContext DbContext { get; } = dbContext;

    public Task<Apu?> GetAsync(
        Guid projectId,
        Guid apuId,
        CancellationToken cancellationToken = default)
    {
        return DbContext.Apus
            .Include(apu => apu.Lines)
            .ThenInclude(line => line.Resource)
            .ThenInclude(resource => resource.ResourceType)
            .SingleOrDefaultAsync(
                apu => apu.ProjectId == projectId && apu.Id == apuId,
                cancellationToken);
    }

    public Task<Apu?> GetByIdAsync(Guid apuId, CancellationToken cancellationToken = default)
    {
        return DbContext.Apus
            .Include(apu => apu.Lines)
            .SingleOrDefaultAsync(apu => apu.Id == apuId, cancellationToken);
    }

    public async Task<Apu?> GetByWbsItemAsync(
        Guid projectId,
        Guid wbsItemId,
        CancellationToken cancellationToken = default)
    {
        var apuId = await DbContext.WbsItems
            .AsNoTracking()
            .Where(item => item.ProjectId == projectId && item.Id == wbsItemId)
            .Select(item => item.ApuId)
            .SingleOrDefaultAsync(cancellationToken);

        return apuId.HasValue
            ? await GetAsync(projectId, apuId.Value, cancellationToken)
            : null;
    }

    public async Task<ApuCostPayload?> GetCostPayloadAsync(
        Guid projectId,
        Guid apuId,
        CancellationToken cancellationToken = default)
    {
        var apu = await DbContext.Apus
            .AsNoTracking()
            .Where(existing => existing.ProjectId == projectId && existing.Id == apuId)
            .Select(existing => new
            {
                existing.Id,
                existing.ProjectId,
                Lines = existing.Lines.Select(line => new
                {
                    line.Id,
                    line.ResourceId,
                    line.Consumption,
                    line.WastePercentage,
                    TypeCode = line.Resource.ResourceType.Code,
                    line.Resource.Description,
                    line.Resource.Unit,
                    line.Resource.UnitPrice
                }).ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (apu is null)
        {
            return null;
        }

        var matrixIds = apu.Lines
            .Where(line => string.Equals(line.TypeCode, ResourceTypeCatalog.Matrix, StringComparison.OrdinalIgnoreCase))
            .Select(line => line.ResourceId)
            .Distinct()
            .ToList();

        var componentsByMatrix = await LoadMatrixComponentsAsync(matrixIds, cancellationToken);
        var matrixUnits = await LoadMatrixUnitsAsync(matrixIds, cancellationToken);

        var lines = apu.Lines
            .Select(line =>
            {
                var isMatrix = string.Equals(
                    line.TypeCode,
                    ResourceTypeCatalog.Matrix,
                    StringComparison.OrdinalIgnoreCase);

                var components = isMatrix && componentsByMatrix.TryGetValue(line.ResourceId, out var found)
                    ? found
                    : [];

                var matrixComplete = !isMatrix ||
                    (matrixUnits.TryGetValue(line.ResourceId, out var unit) &&
                     !string.IsNullOrWhiteSpace(unit) &&
                     components.Count > 0);

                return new ApuCostLineSource(
                    line.Id,
                    line.ResourceId,
                    line.TypeCode,
                    line.Description,
                    line.Unit,
                    line.UnitPrice,
                    line.Consumption,
                    line.WastePercentage,
                    components,
                    matrixComplete);
            })
            .ToList();

        return new ApuCostPayload(apu.Id, apu.ProjectId, lines);
    }

    public async Task<ApuListPage> ListOtherProjectsAsync(
        Guid projectId,
        Guid companyId,
        string? search,
        Guid? departmentId,
        Guid? municipalityId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query =
            from apu in DbContext.Apus.AsNoTracking()
            join project in DbContext.Projects.AsNoTracking() on apu.ProjectId equals project.Id
            where apu.ProjectId != projectId && project.CompanyId == companyId
            select new
            {
                ApuId = apu.Id,
                apu.Description,
                apu.Unit,
                ProjectId = project.Id,
                ProjectName = project.Name,
                project.DepartmentId,
                project.MunicipalityId,
                ItemCode = DbContext.WbsItems
                    .Where(item => item.ApuId == apu.Id)
                    .Select(item => item.Code.Code)
                    .FirstOrDefault()
            };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(row =>
                row.Description.Contains(term) ||
                (row.ItemCode != null && row.ItemCode.Contains(term)));
        }

        if (departmentId.HasValue)
        {
            query = query.Where(row => row.DepartmentId == departmentId.Value);
        }

        if (municipalityId.HasValue)
        {
            query = query.Where(row => row.MunicipalityId == municipalityId.Value);
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderBy(row => row.ProjectName)
            .ThenBy(row => row.ItemCode)
            .ThenBy(row => row.ApuId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var locationNames = await LoadLocationNamesAsync(
            rows.Select(row => row.ProjectId).ToList(),
            cancellationToken);

        var items = new List<ApuListRow>(rows.Count);
        foreach (var row in rows)
        {
            var payload = await GetCostPayloadAsync(row.ProjectId, row.ApuId, cancellationToken);
            locationNames.TryGetValue(row.ProjectId, out var location);

            items.Add(new ApuListRow(
                row.ApuId,
                row.ItemCode ?? string.Empty,
                row.Description,
                row.Unit,
                row.ProjectId,
                row.ProjectName,
                location.MunicipalityName,
                location.DepartmentName,
                payload?.Lines ?? []));
        }

        return new ApuListPage(page, pageSize, totalItems, items);
    }

    public async Task AddAsync(Apu apu, CancellationToken cancellationToken = default)
    {
        await DbContext.Apus.AddAsync(apu, cancellationToken);
    }

    public void RemoveLine(ApuResource line)
    {
        DbContext.ApuResources.Remove(line);
    }

    public Task<Resource?> GetProjectResourceAsync(
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

    public async Task<IReadOnlyList<SimpleResourceSnapshot>> GetProjectSimpleResourcesAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var matrixTypeId = ResourceTypeCatalog.MatrixTypeId;
        var rows = await DbContext.Resources
            .AsNoTracking()
            .Where(resource =>
                resource.ProjectId == projectId &&
                resource.ResourceTypeId != matrixTypeId)
            .Select(resource => new
            {
                resource.Id,
                TypeCode = resource.ResourceType.Code,
                resource.Description,
                resource.Unit,
                resource.UnitPrice,
                resource.QuotationStatus
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new SimpleResourceSnapshot(
                row.Id,
                row.TypeCode,
                row.Description,
                row.Unit,
                row.UnitPrice,
                QuotationStatusCodes.ToCode(row.QuotationStatus)))
            .ToList();
    }

    public async Task<MatrixStateSnapshot?> GetMatrixStateAsync(
        Guid projectId,
        Guid matrixResourceId,
        CancellationToken cancellationToken = default)
    {
        var resource = await DbContext.Resources
            .AsNoTracking()
            .Where(existing => existing.ProjectId == projectId && existing.Id == matrixResourceId)
            .Select(existing => new
            {
                existing.Id,
                existing.ProjectId,
                existing.Description,
                existing.Unit,
                TypeCode = existing.ResourceType.Code,
                existing.RowVersion
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (resource is null)
        {
            return null;
        }

        var componentCount = await DbContext.MatrixComponents
            .AsNoTracking()
            .CountAsync(
                component => component.MatrixResourceId == matrixResourceId,
                cancellationToken);

        return new MatrixStateSnapshot(
            resource.Id,
            resource.ProjectId,
            resource.Description,
            resource.Unit,
            resource.TypeCode,
            componentCount,
            resource.RowVersion);
    }

    public async Task<IReadOnlyList<MatrixComponentRow>> GetMatrixComponentsAsync(
        Guid projectId,
        Guid matrixResourceId,
        CancellationToken cancellationToken = default)
    {
        var rows = await DbContext.MatrixComponents
            .AsNoTracking()
            .Where(component =>
                component.MatrixResourceId == matrixResourceId &&
                component.ComponentResource.ProjectId == projectId)
            .Select(component => new
            {
                component.Id,
                component.MatrixResourceId,
                component.ComponentResourceId,
                TypeCode = component.ComponentResource.ResourceType.Code,
                component.ComponentResource.Description,
                component.ComponentResource.Unit,
                component.ComponentResource.UnitPrice,
                component.Consumption,
                component.WastePercentage
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new MatrixComponentRow(
                row.Id,
                row.MatrixResourceId,
                row.ComponentResourceId,
                row.TypeCode,
                row.Description,
                row.Unit,
                row.UnitPrice,
                row.Consumption,
                row.WastePercentage))
            .ToList();
    }

    public Task<MatrixComponent?> GetMatrixComponentAsync(
        Guid projectId,
        Guid matrixResourceId,
        Guid componentId,
        CancellationToken cancellationToken = default)
    {
        return DbContext.MatrixComponents
            .Include(component => component.ComponentResource)
            .ThenInclude(resource => resource.ResourceType)
            .SingleOrDefaultAsync(
                component =>
                    component.Id == componentId &&
                    component.MatrixResourceId == matrixResourceId &&
                    component.ComponentResource.ProjectId == projectId,
                cancellationToken);
    }

    public async Task AddMatrixComponentAsync(
        MatrixComponent component,
        CancellationToken cancellationToken = default)
    {
        await DbContext.MatrixComponents.AddAsync(component, cancellationToken);
    }

    public void RemoveMatrixComponent(MatrixComponent component)
    {
        DbContext.MatrixComponents.Remove(component);
    }

    public async Task<IReadOnlyList<MatrixComponentSource>> GetMatrixComponentsForCostAsync(
        Guid matrixResourceId,
        CancellationToken cancellationToken = default)
    {
        var rows = await DbContext.MatrixComponents
            .AsNoTracking()
            .Where(component => component.MatrixResourceId == matrixResourceId)
            .Select(component => new
            {
                component.ComponentResourceId,
                TypeCode = component.ComponentResource.ResourceType.Code,
                component.ComponentResource.Description,
                component.ComponentResource.Unit,
                component.ComponentResource.UnitPrice,
                component.Consumption,
                component.WastePercentage
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new MatrixComponentSource(
                row.ComponentResourceId,
                row.TypeCode,
                row.Description,
                row.Unit,
                row.UnitPrice,
                row.Consumption,
                row.WastePercentage))
            .ToList();
    }

    public async Task MarkMatrixChangedAsync(
        Guid matrixResourceId,
        Guid actorUserId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var matrix = await DbContext.Resources
            .SingleOrDefaultAsync(resource => resource.Id == matrixResourceId, cancellationToken);

        if (matrix is null)
        {
            return;
        }

        var componentCount = await DbContext.MatrixComponents
            .CountAsync(
                component => component.MatrixResourceId == matrixResourceId,
                cancellationToken);

        matrix.LastModifiedByUserId = actorUserId;
        matrix.LastModifiedAtUtc = utcNow;
        matrix.QuotationStatus = !string.IsNullOrWhiteSpace(matrix.Unit) && componentCount > 0
            ? QuotationStatus.PorCotizar
            : QuotationStatus.FaltaInformacion;
    }

    public async Task<ProjectCostSettings?> GetProjectSettingsAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var project = await DbContext.Projects
            .AsNoTracking()
            .Where(existing => existing.Id == projectId)
            .Select(existing => new
            {
                existing.Id,
                existing.CompanyId,
                Adm = existing.Configuration == null ? null : existing.Configuration.AiuAdminPercent,
                Unf = existing.Configuration == null ? null : existing.Configuration.AiuUnforeseenPercent,
                Pro = existing.Configuration == null ? null : existing.Configuration.AiuProfitPercent,
                Rounding = existing.Configuration == null
                    ? Domain.Projects.ProjectConfiguration.DefaultDecimals
                    : existing.Configuration.RoundingDecimals,
                Quantity = existing.Configuration == null
                    ? Domain.Projects.ProjectConfiguration.DefaultDecimals
                    : existing.Configuration.QuantityDecimals
            })
            .SingleOrDefaultAsync(cancellationToken);

        return project is null
            ? null
            : new ProjectCostSettings(
                project.Id,
                project.CompanyId,
                project.Adm ?? 0m,
                project.Unf ?? 0m,
                project.Pro ?? 0m,
                project.Rounding,
                project.Quantity);
    }

    public async Task<ResourceUsageData> GetUsageDataAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var resources = await DbContext.Resources
            .AsNoTracking()
            .Where(resource => resource.ProjectId == projectId)
            .Select(resource => new ResourceUsageEntry(
                resource.Id,
                resource.Description,
                resource.Unit,
                resource.ResourceType.Code,
                resource.ResourceType.Name,
                resource.UnitPrice))
            .ToListAsync(cancellationToken);

        var directLines = await (
                from line in DbContext.ApuResources.AsNoTracking()
                join apu in DbContext.Apus.AsNoTracking() on line.ApuId equals apu.Id
                join item in DbContext.WbsItems.AsNoTracking() on apu.Id equals item.ApuId
                where apu.ProjectId == projectId &&
                      line.Resource.ResourceTypeId != ResourceTypeCatalog.MatrixTypeId
                select new UsageDirectLineRow(
                    line.ResourceId,
                    line.Consumption,
                    line.WastePercentage,
                    item.QuantityWork))
            .ToListAsync(cancellationToken);

        var matrixLines = await (
                from line in DbContext.ApuResources.AsNoTracking()
                join apu in DbContext.Apus.AsNoTracking() on line.ApuId equals apu.Id
                join item in DbContext.WbsItems.AsNoTracking() on apu.Id equals item.ApuId
                where apu.ProjectId == projectId &&
                      line.Resource.ResourceTypeId == ResourceTypeCatalog.MatrixTypeId
                select new UsageMatrixLineRow(
                    line.ResourceId,
                    line.Consumption,
                    line.WastePercentage,
                    item.QuantityWork))
            .ToListAsync(cancellationToken);

        var matrixComponents = await (
                from component in DbContext.MatrixComponents.AsNoTracking()
                join matrix in DbContext.Resources.AsNoTracking() on component.MatrixResourceId equals matrix.Id
                where matrix.ProjectId == projectId
                select new UsageMatrixComponentRow(
                    component.MatrixResourceId,
                    component.ComponentResourceId,
                    component.Consumption,
                    component.WastePercentage))
            .ToListAsync(cancellationToken);

        return new ResourceUsageData(resources, directLines, matrixLines, matrixComponents);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return DbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public void SetOriginalRowVersion(Apu apu, byte[] rowVersion)
    {
        DbContext.Entry(apu).Property(entity => entity.RowVersion).OriginalValue = rowVersion;
    }

    public void SetOriginalRowVersion(Resource resource, byte[] rowVersion)
    {
        DbContext.Entry(resource).Property(entity => entity.RowVersion).OriginalValue = rowVersion;
    }

    private async Task<IReadOnlyDictionary<Guid, IReadOnlyList<MatrixComponentSource>>>
        LoadMatrixComponentsAsync(
            IReadOnlyList<Guid> matrixIds,
            CancellationToken cancellationToken)
    {
        if (matrixIds.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<MatrixComponentSource>>();
        }

        var rows = await DbContext.MatrixComponents
            .AsNoTracking()
            .Where(component => matrixIds.Contains(component.MatrixResourceId))
            .Select(component => new
            {
                component.MatrixResourceId,
                component.ComponentResourceId,
                TypeCode = component.ComponentResource.ResourceType.Code,
                component.ComponentResource.Description,
                component.ComponentResource.Unit,
                component.ComponentResource.UnitPrice,
                component.Consumption,
                component.WastePercentage
            })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => row.MatrixResourceId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<MatrixComponentSource>)group
                    .Select(row => new MatrixComponentSource(
                        row.ComponentResourceId,
                        row.TypeCode,
                        row.Description,
                        row.Unit,
                        row.UnitPrice,
                        row.Consumption,
                        row.WastePercentage))
                    .ToList());
    }

    private async Task<IReadOnlyDictionary<Guid, string?>> LoadMatrixUnitsAsync(
        IReadOnlyList<Guid> matrixIds,
        CancellationToken cancellationToken)
    {
        if (matrixIds.Count == 0)
        {
            return new Dictionary<Guid, string?>();
        }

        return await DbContext.Resources
            .AsNoTracking()
            .Where(resource => matrixIds.Contains(resource.Id))
            .Select(resource => new { resource.Id, resource.Unit })
            .ToDictionaryAsync(row => row.Id, row => row.Unit, cancellationToken);
    }

    private async Task<IReadOnlyDictionary<Guid, ProjectLocation>> LoadLocationNamesAsync(
        IReadOnlyList<Guid> projectIds,
        CancellationToken cancellationToken)
    {
        if (projectIds.Count == 0)
        {
            return new Dictionary<Guid, ProjectLocation>();
        }

        var rows = await (
                from project in DbContext.Projects.AsNoTracking()
                join municipality in DbContext.Municipalities.AsNoTracking()
                    on project.MunicipalityId equals municipality.Id
                join department in DbContext.Departments.AsNoTracking()
                    on municipality.DepartmentId equals department.Id
                where projectIds.Contains(project.Id)
                select new
                {
                    project.Id,
                    MunicipalityName = municipality.Name,
                    DepartmentName = department.Name
                })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(
            row => row.Id,
            row => new ProjectLocation(row.MunicipalityName, row.DepartmentName));
    }

    private sealed record ProjectLocation(string? MunicipalityName, string? DepartmentName);
}
