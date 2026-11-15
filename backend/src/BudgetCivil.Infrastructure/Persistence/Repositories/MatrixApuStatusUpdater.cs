using BudgetCivil.Application.Resources.Repositories;
using BudgetCivil.Domain.Resources;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Infrastructure.Persistence.Repositories;

/// <summary>
/// Recálculo de un solo nivel del estado de los APUs que usan una Matriz (`D4`, `SC-APUS-100`,
/// `INC-06`). No altera la autoría ni invalida aprobaciones (`D-53`, `E-4b`).
/// </summary>
public sealed class MatrixApuStatusUpdater(BudgetCivilDbContext dbContext) : IMatrixApuStatusUpdater
{
    public async Task RecalculateMatrixAsync(
        Guid projectId,
        Guid matrixResourceId,
        CancellationToken cancellationToken = default)
    {
        var matrix = await dbContext.Resources
            .SingleOrDefaultAsync(
                resource => resource.ProjectId == projectId && resource.Id == matrixResourceId,
                cancellationToken);

        if (matrix is null)
        {
            return;
        }

        var componentCount = await dbContext.MatrixComponents
            .AsNoTracking()
            .CountAsync(
                component => component.MatrixResourceId == matrixResourceId,
                cancellationToken);

        matrix.QuotationStatus = !string.IsNullOrWhiteSpace(matrix.Unit) && componentCount > 0
            ? QuotationStatus.PorCotizar
            : QuotationStatus.FaltaInformacion;

        await RecalculateForMatrixAsync(projectId, matrixResourceId, cancellationToken);
    }

    public async Task RecalculateForMatrixAsync(
        Guid projectId,
        Guid matrixResourceId,
        CancellationToken cancellationToken = default)
    {
        var apus = await dbContext.Apus
            .Include(apu => apu.Lines)
            .ThenInclude(line => line.Resource)
            .ThenInclude(resource => resource.ResourceType)
            .Where(apu =>
                apu.ProjectId == projectId &&
                apu.Lines.Any(line => line.ResourceId == matrixResourceId))
            .ToListAsync(cancellationToken);

        if (apus.Count == 0)
        {
            return;
        }

        var matrixIds = apus
            .SelectMany(apu => apu.Lines)
            .Where(line => string.Equals(
                line.Resource.ResourceType.Code,
                ResourceTypeCatalog.Matrix,
                StringComparison.OrdinalIgnoreCase))
            .Select(line => line.ResourceId)
            .Distinct()
            .ToList();

        var componentCounts = await dbContext.MatrixComponents
            .AsNoTracking()
            .Where(component => matrixIds.Contains(component.MatrixResourceId))
            .GroupBy(component => component.MatrixResourceId)
            .Select(group => new { MatrixResourceId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.MatrixResourceId, row => row.Count, cancellationToken);

        foreach (var apu in apus)
        {
            var matricesComplete = apu.Lines
                .Where(line => string.Equals(
                    line.Resource.ResourceType.Code,
                    ResourceTypeCatalog.Matrix,
                    StringComparison.OrdinalIgnoreCase))
                .All(line =>
                    !string.IsNullOrWhiteSpace(line.Resource.Unit) &&
                    componentCounts.TryGetValue(line.ResourceId, out var count) &&
                    count > 0);

            apu.CalculateStatus(matricesComplete);
        }
    }
}
