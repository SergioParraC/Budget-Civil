using BudgetCivil.Application.Apus.Dtos;
using BudgetCivil.Application.Apus.Repositories;
using BudgetCivil.Application.Apus.Services;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Domain.Resources;

namespace BudgetCivil.Application.Apus.Queries;

/// <summary>
/// Composición y costo directo de la Matriz (`REQ-APUS-20`, `SC-APUS-92`, `93`). Una Matriz de otra
/// compañía responde `MATRIX_NOT_FOUND` (`SC-APUS-83`).
/// </summary>
public sealed class GetMatrixComponentsQueryHandler(
    ApuAccessService apuAccessService,
    IApuRepository apuRepository,
    MatrixDetailService matrixDetailService)
{
    public async Task<MatrixDetailResponse> ExecuteAsync(
        Guid actorUserId,
        Guid projectId,
        Guid matrixResourceId,
        CancellationToken cancellationToken = default)
    {
        await apuAccessService.EnsureReadAsync(actorUserId, projectId, cancellationToken);

        var matrix = await apuRepository.GetProjectResourceAsync(
            projectId,
            matrixResourceId,
            cancellationToken)
            ?? throw new NotFoundAppException("La Matriz no existe en el proyecto.", ApuRules.MatrixNotFound);

        if (!string.Equals(matrix.ResourceType?.Code, ResourceTypeCatalog.Matrix, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationAppException("El insumo no es de tipo Matriz.", ApuRules.MatrixTypeInvalid);
        }

        return await matrixDetailService.BuildAsync(projectId, matrixResourceId, cancellationToken);
    }
}
