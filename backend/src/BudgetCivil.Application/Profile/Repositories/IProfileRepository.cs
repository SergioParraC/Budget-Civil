using BudgetCivil.Domain.Users.Entities;

namespace BudgetCivil.Application.Repositories;

public interface IProfileRepository
{
    Task<UserAccount?> GetActorByIdAsync(Guid actorUserId, CancellationToken cancellationToken = default);

    Task<UserAccount?> GetUserWithProfileByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, Guid excludingUserId, CancellationToken cancellationToken = default);

    Task<bool> UsernameExistsAsync(string username, Guid excludingUserId, CancellationToken cancellationToken = default);

    Task<bool> DocumentExistsAsync(
        string documentType,
        string documentNumber,
        Guid excludingUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PasswordRecoveryGrant>> GetActiveRecoveryGrantsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddRecoveryGrantAsync(PasswordRecoveryGrant grant, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
