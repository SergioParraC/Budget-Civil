namespace BudgetCivil.Application.Apus.Repositories;

public sealed record ApuProjectAccess(Guid ProjectId, Guid CompanyId, bool IsMember);

/// <summary>Resuelve la compañía y la pertenencia del actor sin exponer la persistencia a `M-05` (`D6`).</summary>
public interface IApuProjectAccessReader
{
    Task<ApuProjectAccess?> GetAccessAsync(
        Guid projectId,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}
