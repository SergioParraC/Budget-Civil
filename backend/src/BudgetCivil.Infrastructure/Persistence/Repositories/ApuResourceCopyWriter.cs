using BudgetCivil.Application.Apus.Repositories;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Domain.Apus;
using BudgetCivil.Domain.Resources;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Infrastructure.Persistence.Repositories;

/// <summary>
/// Copia de insumos y Matrices en el mismo `DbContext` y la misma transacción (`D9`, `D-54`).
/// </summary>
public sealed class ApuResourceCopyWriter(BudgetCivilDbContext dbContext) : IApuResourceCopyWriter
{
    public Task<Resource?> GetResourceAsync(
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Resources
            .Include(resource => resource.ResourceType)
            .SingleOrDefaultAsync(resource => resource.Id == resourceId, cancellationToken);
    }

    public Task<Resource> CopyResourceAsync(
        Resource source,
        Guid targetProjectId,
        Guid actorUserId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        return Task.FromResult(source.CopyTo(targetProjectId, actorUserId, utcNow));
    }

    public async Task AddResourceAsync(Resource resource, CancellationToken cancellationToken = default)
    {
        await dbContext.Resources.AddAsync(resource, cancellationToken);
    }

    public async Task<MatrixCopyResult> CopyMatrixAsync(
        Guid sourceMatrixResourceId,
        Guid targetProjectId,
        Guid actorUserId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var source = await dbContext.Resources
            .Include(resource => resource.ResourceType)
            .SingleOrDefaultAsync(
                resource => resource.Id == sourceMatrixResourceId,
                cancellationToken)
            ?? throw new NotFoundAppException("La Matriz no existe.", "MATRIX_NOT_FOUND");

        var components = await dbContext.MatrixComponents
            .Include(component => component.ComponentResource)
            .Where(component => component.MatrixResourceId == sourceMatrixResourceId)
            .ToListAsync(cancellationToken);

        var matrixCopy = source.CopyTo(targetProjectId, actorUserId, utcNow);
        await dbContext.Resources.AddAsync(matrixCopy, cancellationToken);

        var componentResourceMap = new Dictionary<Guid, Guid>();

        foreach (var component in components)
        {
            var componentCopy = component.ComponentResource.CopyTo(targetProjectId, actorUserId, utcNow);
            await dbContext.Resources.AddAsync(componentCopy, cancellationToken);

            var componentCopyLine = MatrixComponent.Create(
                matrixCopy.Id,
                componentCopy.Id,
                component.Consumption,
                component.WastePercentage);

            await dbContext.MatrixComponents.AddAsync(componentCopyLine, cancellationToken);
            componentResourceMap[component.ComponentResourceId] = componentCopy.Id;
        }

        matrixCopy.QuotationStatus = !string.IsNullOrWhiteSpace(matrixCopy.Unit) &&
                                      componentResourceMap.Count > 0
            ? QuotationStatus.PorCotizar
            : QuotationStatus.FaltaInformacion;

        return new MatrixCopyResult(matrixCopy.Id, componentResourceMap);
    }
}
