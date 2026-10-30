using BudgetCivil.Domain.Users.Entities;

namespace BudgetCivil.Application.Memberships.Repositories;

public interface ICompanyMembershipRepository
{
    Task<UserAccount?> GetActorByIdAsync(Guid actorUserId, CancellationToken cancellationToken = default);

    Task<UserAccount?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Company?> GetCompanyByIdAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asigna la compañía al usuario solo si aún no tiene una (`D-98`). La condición se evalúa en la
    /// misma sentencia, de modo que dos asignaciones simultáneas no dejan dos compañías efectivas.
    /// </summary>
    Task<bool> TryAssignCompanyAsync(
        Guid userId,
        Guid companyId,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retira la compañía indicada al usuario si es la que tiene asignada.
    /// </summary>
    Task<bool> TryUnassignCompanyAsync(
        Guid userId,
        Guid companyId,
        DateTime utcNow,
        CancellationToken cancellationToken = default);
}
