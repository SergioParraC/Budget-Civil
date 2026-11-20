using BudgetCivil.Application.Calculation;
using BudgetCivil.Application.Calculation.Repositories;
using BudgetCivil.Domain.Projects;
using BudgetCivil.Domain.Resources;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Infrastructure.Persistence.Repositories;

/// <summary>
/// Lectura set-based del presupuesto (`D6`, `REQ-CALCULO-05`): ítems del EDT, APUs con líneas y Matrices
/// expandidas, configuración, divisa y tope en consultas por `project_id`, sin N+1.
/// </summary>
public sealed class ProjectCostReader(BudgetCivilDbContext dbContext) : IApuCostReader
{
    public async Task<ProjectCostData?> GetProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var project = await dbContext.Projects
            .AsNoTracking()
            .Where(existingProject => existingProject.Id == projectId)
            .Select(existingProject => new { existingProject.Id, existingProject.CompanyId })
            .SingleOrDefaultAsync(cancellationToken);

        if (project is null)
        {
            return null;
        }

        var configuration = await dbContext.ProjectConfigurations
            .AsNoTracking()
            .Where(existing => existing.ProjectId == projectId)
            .Select(existing => new
            {
                existing.BaseCurrencyId,
                existing.BudgetLimit,
                existing.AiuAdminPercent,
                existing.AiuUnforeseenPercent,
                existing.AiuProfitPercent,
                existing.RoundingDecimals,
                existing.QuantityDecimals
            })
            .SingleOrDefaultAsync(cancellationToken);

        ProjectCostCurrency? currency = null;
        if (configuration is not null)
        {
            currency = await dbContext.BaseCurrencies
                .AsNoTracking()
                .Where(existing => existing.Id == configuration.BaseCurrencyId)
                .Select(existing => new ProjectCostCurrency(existing.Id, existing.IsoCode, existing.Name))
                .SingleOrDefaultAsync(cancellationToken);
        }

        var settings = new CostSettings(
            configuration?.RoundingDecimals ?? ProjectConfiguration.DefaultDecimals,
            configuration?.QuantityDecimals ?? ProjectConfiguration.DefaultDecimals,
            configuration?.AiuAdminPercent ?? 0m,
            configuration?.AiuUnforeseenPercent ?? 0m,
            configuration?.AiuProfitPercent ?? 0m);

        var items = await dbContext.WbsItems
            .AsNoTracking()
            .Where(item => item.ProjectId == projectId)
            .Select(item => new ProjectCostItem(
                item.Id,
                item.ApuId,
                item.QuantityWork,
                item.WbsChapterId,
                item.OrderIndex))
            .ToListAsync(cancellationToken);

        var apuRows = await dbContext.Apus
            .AsNoTracking()
            .Where(apu => apu.ProjectId == projectId)
            .Select(apu => new
            {
                apu.Id,
                Lines = apu.Lines.Select(line => new
                {
                    line.Id,
                    line.ResourceId,
                    TypeCode = line.Resource.ResourceType.Code,
                    line.Resource.Description,
                    line.Resource.Unit,
                    line.Resource.UnitPrice,
                    line.Consumption,
                    line.WastePercentage
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        var matrixIds = apuRows
            .SelectMany(apu => apu.Lines)
            .Where(line => IsMatrix(line.TypeCode))
            .Select(line => line.ResourceId)
            .Distinct()
            .ToList();

        var componentsByMatrix = await LoadMatrixComponentsAsync(matrixIds, cancellationToken);
        var unitsByMatrix = await LoadMatrixUnitsAsync(matrixIds, cancellationToken);

        var apuLines = apuRows.ToDictionary(
            apu => apu.Id,
            apu => (IReadOnlyList<ApuCostLineInput>)apu.Lines
                .Select(line =>
                {
                    IReadOnlyList<MatrixComponentInput>? components = null;

                    if (IsMatrix(line.TypeCode) &&
                        unitsByMatrix.TryGetValue(line.ResourceId, out var unit) &&
                        !string.IsNullOrWhiteSpace(unit) &&
                        componentsByMatrix.TryGetValue(line.ResourceId, out var found) &&
                        found.Count > 0)
                    {
                        components = found;
                    }

                    return new ApuCostLineInput(
                        line.Id,
                        line.ResourceId,
                        line.TypeCode,
                        line.Description,
                        line.Unit,
                        line.UnitPrice,
                        line.Consumption,
                        line.WastePercentage,
                        components);
                })
                .ToList());

        return new ProjectCostData(
            project.Id,
            project.CompanyId,
            settings,
            configuration?.BudgetLimit,
            currency,
            items,
            apuLines);
    }

    private static bool IsMatrix(string? resourceTypeCode)
    {
        return string.Equals(resourceTypeCode, ResourceTypeCatalog.Matrix, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<IReadOnlyDictionary<Guid, IReadOnlyList<MatrixComponentInput>>>
        LoadMatrixComponentsAsync(
            IReadOnlyList<Guid> matrixIds,
            CancellationToken cancellationToken)
    {
        if (matrixIds.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<MatrixComponentInput>>();
        }

        var rows = await dbContext.MatrixComponents
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
                group => (IReadOnlyList<MatrixComponentInput>)group
                    .Select(row => new MatrixComponentInput(
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

        return await dbContext.Resources
            .AsNoTracking()
            .Where(resource => matrixIds.Contains(resource.Id))
            .Select(resource => new { resource.Id, resource.Unit })
            .ToDictionaryAsync(row => row.Id, row => row.Unit, cancellationToken);
    }
}

