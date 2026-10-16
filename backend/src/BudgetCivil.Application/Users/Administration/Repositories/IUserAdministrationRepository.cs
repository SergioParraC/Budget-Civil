using BudgetCivil.Domain.Users.Entities;

namespace BudgetCivil.Application.Users.Administration.Repositories;

public interface IUserAdministrationRepository
{
    Task<UserAccount?> GetActorByIdAsync(Guid actorUserId, CancellationToken cancellationToken = default);

    Task<UserAccount?> GetUserForManagementAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Role?> GetRoleByCodeAsync(string roleCode, CancellationToken cancellationToken = default);

    Task<Company?> GetActiveCompanyByIdAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, Guid? excludingUserId = null, CancellationToken cancellationToken = default);

    Task<bool> UsernameExistsAsync(string username, Guid? excludingUserId = null, CancellationToken cancellationToken = default);

    Task<bool> DocumentExistsAsync(
        string documentType,
        string documentNumber,
        Guid? excludingUserId = null,
        CancellationToken cancellationToken = default);

    Task<int> CountEnabledAdministratorsAsync(CancellationToken cancellationToken = default);

    Task<UserCompanyMembership?> GetActiveMembershipAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddUserAsync(UserAccount userAccount, CancellationToken cancellationToken = default);

    Task AddMembershipAsync(UserCompanyMembership membership, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
