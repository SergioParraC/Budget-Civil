using BudgetCivil.Application.Calculation.Dtos;
using BudgetCivil.Application.Calculation.Services;
using BudgetCivil.Application.Common.Exceptions;

namespace BudgetCivil.Application.Calculation.Queries;

/// <summary>
/// Consulta del presupuesto del proyecto (`REQ-CALCULO-01`…`05`, `D4`). Aplica el alcance de lectura del
/// árbol y deriva el cálculo en cada lectura; no persiste costos.
/// </summary>
public sealed class GetProjectBudgetQueryHandler(
    CalculationAccessService calculationAccessService,
    ApuCostService apuCostService)
{
    public async Task<ProjectBudgetResponse> ExecuteAsync(
        Guid actorUserId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        await calculationAccessService.EnsureReadAsync(actorUserId, projectId, cancellationToken);

        var snapshot = await apuCostService.GetProjectSnapshotAsync(projectId, cancellationToken)
            ?? throw new NotFoundAppException("Project was not found.", "PROJECT_NOT_FOUND");

        var budget = ProjectBudgetCalculator.Calculate(snapshot);
        return ProjectBudgetMapper.ToResponse(budget, snapshot);
    }
}
