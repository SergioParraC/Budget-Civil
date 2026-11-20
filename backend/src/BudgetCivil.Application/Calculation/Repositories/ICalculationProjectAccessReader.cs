namespace BudgetCivil.Application.Calculation.Repositories;

public sealed record CalculationProjectAccess(Guid ProjectId, Guid CompanyId, bool IsMember);

/// <summary>
/// Resuelve la compañía del proyecto y la pertenencia del actor sin depender de `Application.Wbs` ni de
/// `Application.Apus` (`D5`, `REQ-CALCULO-04`).
/// </summary>
public interface ICalculationProjectAccessReader
{
    Task<CalculationProjectAccess?> GetAccessAsync(
        Guid projectId,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}
