using BudgetCivil.Application.Users.Auth.Repositories;
using BudgetCivil.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Infrastructure.Persistence.Repositories;

public sealed class EmailVerificationTokenRepository(BudgetCivilDbContext dbContext) : IEmailVerificationTokenRepository
{
    public Task AddTokenAsync(EmailVerificationToken token, CancellationToken cancellationToken = default)
    {
        return dbContext.EmailVerificationTokens.AddAsync(token, cancellationToken).AsTask();
    }

    public Task<EmailVerificationToken?> FindTokenByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return dbContext.EmailVerificationTokens
            .Include(token => token.UserAccount)
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
