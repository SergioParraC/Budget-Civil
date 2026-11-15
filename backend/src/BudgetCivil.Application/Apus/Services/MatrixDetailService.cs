using BudgetCivil.Application.Apus.Dtos;
using BudgetCivil.Application.Apus.Repositories;
using BudgetCivil.Application.Calculation;
using BudgetCivil.Application.Common.Exceptions;

namespace BudgetCivil.Application.Apus.Services;

/// <summary>Vista de la composición y el costo directo de una Matriz (`REQ-APUS-20`, `D5`).</summary>
public sealed class MatrixDetailService(
    IApuRepository apuRepository)
{
    public async Task<MatrixDetailResponse> BuildAsync(
        Guid projectId,
        Guid matrixResourceId,
        CancellationToken cancellationToken = default)
    {
        var state = await apuRepository.GetMatrixStateAsync(projectId, matrixResourceId, cancellationToken)
            ?? throw new NotFoundAppException("La Matriz no existe en el proyecto.", ApuRules.MatrixNotFound);

        if (!string.Equals(state.ResourceTypeCode, "MATRIX", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationAppException("El insumo no es de tipo Matriz.", ApuRules.MatrixTypeInvalid);
        }

        var components = await apuRepository.GetMatrixComponentsAsync(projectId, matrixResourceId, cancellationToken);
        var settings = await apuRepository.GetProjectSettingsAsync(projectId, cancellationToken)
            ?? throw new NotFoundAppException("Project was not found.", "PROJECT_NOT_FOUND");

        var cost = ApuCostCalculator.CalculateMatrix(
            ToComponentInputs(components),
            ApuDetailService.ToCostSettings(settings));

        return ApuMapper.ToMatrixDetail(state, cost, components);
    }

    public static IReadOnlyList<MatrixComponentInput> ToComponentInputs(
        IReadOnlyList<MatrixComponentRow> components)
    {
        return components
            .Select(component => new MatrixComponentInput(
                component.ComponentResourceId,
                component.ComponentResourceTypeCode,
                component.Description,
                component.Unit,
                component.UnitPrice,
                component.Consumption,
                component.WastePercentage))
            .ToList();
    }
}
