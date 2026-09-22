using BudgetCivil.Application.Users.Administration.Repositories;
using BudgetCivil.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Infrastructure.Persistence.Repositories;

public sealed class UserAdministrationRepository(BudgetCivilDbContext dbContext) : IUserAdministrationRepository
{
    public Task<UserAccount?> GetActorByIdAsync(Guid actorUserId, CancellationToken cancellationToken = default)
    {
        return dbContext.UserAccounts
            .Include(user => user.Role)
            .SingleOrDefaultAsync(user => user.Id == actorUserId, cancellationToken);
    }

    public Task<UserAccount?> GetUserForManagementAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return dbContext.UserAccounts
            .Include(user => user.Role)
            .Include(user => user.Profile)
            .Include(user => user.Memberships)
            .SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);
    }

    public Task<Role?> GetRoleByCodeAsync(string roleCode, CancellationToken cancellationToken = default)
    {
        var normalizedRoleCode = roleCode.Trim().ToUpperInvariant();

        return dbContext.Roles
            .SingleOrDefaultAsync(role => role.Code == normalizedRoleCode, cancellationToken);
    }

    public Task<Company?> GetActiveCompanyByIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return dbContext.Companies
            .SingleOrDefaultAsync(company => company.Id == companyId && company.IsActive, cancellationToken);
    }

    public Task<bool> EmailExistsAsync(string email, Guid? excludingUserId = null, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();

        return dbContext.UserAccounts.AnyAsync(
            user =>
                user.Email.ToUpper() == normalizedEmail &&
                (!excludingUserId.HasValue || user.Id != excludingUserId.Value),
            cancellationToken);
    }

    public Task<bool> UsernameExistsAsync(string username, Guid? excludingUserId = null, CancellationToken cancellationToken = default)
    {
        var normalizedUsername = username.Trim().ToUpperInvariant();

        return dbContext.UserAccounts.AnyAsync(
            user =>
                user.Username.ToUpper() == normalizedUsername &&
                (!excludingUserId.HasValue || user.Id != excludingUserId.Value),
            cancellationToken);
    }

    public Task<bool> DocumentExistsAsync(
        string documentType,
        string documentNumber,
        Guid? excludingUserId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedDocumentType = documentType.Trim().ToUpperInvariant();
        var normalizedDocumentNumber = documentNumber.Trim().ToUpperInvariant();

        return dbContext.UserProfiles.AnyAsync(
            profile =>
                profile.DocumentType.ToUpper() == normalizedDocumentType &&
                profile.DocumentNumber.ToUpper() == normalizedDocumentNumber &&
                (!excludingUserId.HasValue || profile.UserId != excludingUserId.Value),
            cancellationToken);
    }

    public Task<int> CountEnabledAdministratorsAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.UserAccounts
            .CountAsync(
                user =>
                    user.Role.Code == "ADMINISTRADOR" &&
                    user.AccountEnabled,
                cancellationToken);
    }

    public Task<UserCompanyMembership?> GetActiveMembershipAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return dbContext.UserCompanyMemberships
            .SingleOrDefaultAsync(
                membership => membership.UserId == userId && membership.IsActive,
                cancellationToken);
    }

    public Task AddUserAsync(UserAccount userAccount, CancellationToken cancellationToken = default)
    {
        dbContext.UserAccounts.Add(userAccount);
        return Task.CompletedTask;
    }

    public Task AddMembershipAsync(UserCompanyMembership membership, CancellationToken cancellationToken = default)
    {
        dbContext.UserCompanyMemberships.Add(membership);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
