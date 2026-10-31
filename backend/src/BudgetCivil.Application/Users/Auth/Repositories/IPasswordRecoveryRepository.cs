using BudgetCivil.Domain.Users.Entities;

namespace BudgetCivil.Application.Users.Auth.Repositories;

public interface IPasswordRecoveryRepository
{
    Task<UserAccount?> FindUserByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task AddTokenAsync(PasswordRecoveryToken token, CancellationToken cancellationToken = default);

    Task<PasswordRecoveryToken?> FindTokenByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}