namespace BudgetCivil.Application.Wbs.Repositories;

public sealed record WbsProjectAccess(Guid ProjectId, Guid CompanyId, bool IsMember);

/// <summary>
/// Resuelve la compañía del proyecto y la pertenencia del actor sin exponer la persistencia al módulo
/// `M-03` (`PLAN §3.4.1`, `D-54`).
/// </summary>
public interface IWbsProjectAccessReader
{
    Task<WbsProjectAccess?> GetAccessAsync(
        Guid projectId,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}
