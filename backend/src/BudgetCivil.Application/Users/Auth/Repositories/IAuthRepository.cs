using BudgetCivil.Domain.Users.Entities;

namespace BudgetCivil.Application.Users.Auth.Repositories;

public interface IAuthRepository
{
    Task<Role?> GetRoleByCodeAsync(string roleCode, CancellationToken cancellationToken = default);

    Task<UserAccount?> FindByIdentifierAsync(string identifier, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default);

    Task<bool> DocumentExistsAsync(string documentType, string documentNumber, CancellationToken cancellationToken = default);

    Task AddUserAsync(UserAccount userAccount, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetActivePermissionKeysAsync(Guid roleId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
