using BudgetCivil.Application.Users.Auth.Repositories;
using BudgetCivil.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Infrastructure.Persistence.Repositories;

public sealed class PasswordRecoveryRepository(BudgetCivilDbContext dbContext) : IPasswordRecoveryRepository
{
    public Task<UserAccount?> FindUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToUpperInvariant();

        return dbContext.UserAccounts
            .SingleOrDefaultAsync(user => user.Email.ToUpper() == normalized, cancellationToken);
    }

    public async Task AddTokenAsync(PasswordRecoveryToken token, CancellationToken cancellationToken = default)
    {
        await dbContext.PasswordRecoveryTokens.AddAsync(token, cancellationToken);
    }

    public Task<PasswordRecoveryToken?> FindTokenByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return dbContext.PasswordRecoveryTokens
            .Include(token => token.UserAccount)
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}