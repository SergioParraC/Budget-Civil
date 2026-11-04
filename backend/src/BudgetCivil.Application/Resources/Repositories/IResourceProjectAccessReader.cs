namespace BudgetCivil.Application.Resources.Repositories;

public sealed record ResourceProjectAccess(Guid ProjectId, Guid CompanyId, bool IsMember);

/// <summary>
/// Resuelve la compañía del proyecto y la pertenencia del actor sin exponer la persistencia al módulo
/// `M-04` (`PLAN §3.4.1`).
/// </summary>
public interface IResourceProjectAccessReader
{
    Task<ResourceProjectAccess?> GetAccessAsync(
        Guid projectId,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}
