using BudgetCivil.Application.Repositories;
using BudgetCivil.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Infrastructure.Persistence.Repositories;

public sealed class ProfileRepository(BudgetCivilDbContext dbContext) : IProfileRepository
{
    public Task<UserAccount?> GetActorByIdAsync(Guid actorUserId, CancellationToken cancellationToken = default)
    {
        return dbContext.UserAccounts
            .Include(user => user.Role)
            .SingleOrDefaultAsync(user => user.Id == actorUserId, cancellationToken);
    }

    public Task<UserAccount?> GetUserWithProfileByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return dbContext.UserAccounts
            .Include(user => user.Role)
            .Include(user => user.Country)
            .Include(user => user.Department)
            .Include(user => user.Municipality)
            .SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);
    }

    public Task<bool> EmailExistsAsync(string email, Guid excludingUserId, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();

        return dbContext.UserAccounts.AnyAsync(
            user =>
                user.Email.ToUpper() == normalizedEmail &&
                user.Id != excludingUserId,
            cancellationToken);
    }

    public Task<bool> UsernameExistsAsync(string username, Guid excludingUserId, CancellationToken cancellationToken = default)
    {
        var normalizedUsername = username.Trim().ToUpperInvariant();

        return dbContext.UserAccounts.AnyAsync(
            user =>
                user.Username.ToUpper() == normalizedUsername &&
                user.Id != excludingUserId,
            cancellationToken);
    }

    public Task<bool> DocumentExistsAsync(
        string documentType,
        string documentNumber,
        Guid excludingUserId,
        CancellationToken cancellationToken = default)
    {
        var normalizedDocumentType = documentType.Trim().ToUpperInvariant();
        var normalizedDocumentNumber = documentNumber.Trim().ToUpperInvariant();

        return dbContext.UserAccounts.AnyAsync(
            profile =>
                profile.DocumentType.ToUpper() == normalizedDocumentType &&
                profile.DocumentNumber.ToUpper() == normalizedDocumentNumber &&
                profile.Id != excludingUserId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<PasswordRecoveryGrant>> GetActiveRecoveryGrantsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.PasswordRecoveryGrants
            .Where(grant => grant.UserId == userId && grant.IsActive)
            .ToListAsync(cancellationToken);
    }

    public Task AddRecoveryGrantAsync(PasswordRecoveryGrant grant, CancellationToken cancellationToken = default)
    {
        dbContext.PasswordRecoveryGrants.Add(grant);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
