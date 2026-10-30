using BudgetCivil.Domain.Users.Entities;

namespace BudgetCivil.Application.Users.Auth.Repositories;

public interface IEmailVerificationTokenRepository
{
    Task AddTokenAsync(EmailVerificationToken token, CancellationToken cancellationToken = default);

    Task<EmailVerificationToken?> FindTokenByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
