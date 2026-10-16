using BudgetCivil.Application.Users.Auth.Repositories;
using BudgetCivil.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Infrastructure.Persistence.Repositories;

public sealed class AuthRepository(BudgetCivilDbContext dbContext) : IAuthRepository
{
    public Task<Role?> GetRoleByCodeAsync(string roleCode, CancellationToken cancellationToken = default)
    {
        return dbContext.Roles
            .SingleOrDefaultAsync(
                role => role.Code == roleCode,
                cancellationToken);
    }

    public Task<UserAccount?> FindByIdentifierAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var normalizedIdentifier = identifier.Trim().ToUpperInvariant();

        return dbContext.UserAccounts
            .Include(user => user.Role)
            .SingleOrDefaultAsync(
                user =>
                    user.Email.ToUpper() == normalizedIdentifier ||
                    user.Username.ToUpper() == normalizedIdentifier,
                cancellationToken);
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();
        return dbContext.UserAccounts
            .AnyAsync(user => user.Email.ToUpper() == normalizedEmail, cancellationToken);
    }

    public Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalizedUsername = username.Trim().ToUpperInvariant();
        return dbContext.UserAccounts
            .AnyAsync(user => user.Username.ToUpper() == normalizedUsername, cancellationToken);
    }

    public Task<bool> DocumentExistsAsync(string documentType, string documentNumber, CancellationToken cancellationToken = default)
    {
        var normalizedDocumentType = documentType.Trim().ToUpperInvariant();
        var normalizedDocumentNumber = documentNumber.Trim().ToUpperInvariant();

        return dbContext.UserProfiles.AnyAsync(
            profile =>
                profile.DocumentType.ToUpper() == normalizedDocumentType &&
                profile.DocumentNumber.ToUpper() == normalizedDocumentNumber,
            cancellationToken);
    }

    public async Task AddUserAsync(UserAccount userAccount, CancellationToken cancellationToken = default)
    {
        dbContext.UserAccounts.Add(userAccount);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetActivePermissionKeysAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        return await dbContext.RolePermissions
            .AsNoTracking()
            .Where(rolePermission =>
                rolePermission.RoleId == roleId &&
                rolePermission.IsActive &&
                rolePermission.Permission.IsActive)
            .Select(rolePermission => rolePermission.Permission.PermissionKey)
            .Distinct()
            .OrderBy(permissionKey => permissionKey)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
