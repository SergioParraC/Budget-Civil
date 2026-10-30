using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Users.Search;
using BudgetCivil.Application.Users.Search.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Infrastructure.Persistence.Repositories;

public sealed class UserSearchRepository(BudgetCivilDbContext dbContext) : IUserSearchRepository
{
    public async Task<IReadOnlyList<UserSearchResult>> SearchAsync(
        Guid actorUserId,
        UserSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        var actorSnapshot = await dbContext.UserAccounts
            .AsNoTracking()
            .Where(user => user.Id == actorUserId)
            .Select(user => new
            {
                user.CompanyId,
                RoleCode = user.Role.Code
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (actorSnapshot is null)
        {
            throw new ForbiddenAppException("Authenticated user does not exist.", "AUTH_USER_NOT_FOUND");
        }

        var searchQuery = dbContext.UserAccounts
            .AsNoTracking()
            .Include(user => user.Role)
            .AsQueryable();

        if (actorSnapshot.RoleCode != "ADMINISTRADOR")
        {
            if (!actorSnapshot.CompanyId.HasValue)
            {
                throw new ForbiddenAppException("Company context is required for this role.", "COMPANY_CONTEXT_REQUIRED");
            }

            searchQuery = searchQuery.Where(user => user.CompanyId == actorSnapshot.CompanyId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Email))
        {
            var normalizedEmail = query.Email.Trim().ToUpperInvariant();
            searchQuery = searchQuery.Where(user => user.Email.ToUpper().Contains(normalizedEmail));
        }

        if (!string.IsNullOrWhiteSpace(query.Username))
        {
            var normalizedUsername = query.Username.Trim().ToUpperInvariant();
            searchQuery = searchQuery.Where(user => user.Username.ToUpper().Contains(normalizedUsername));
        }

        if (!string.IsNullOrWhiteSpace(query.FullName))
        {
            var normalizedFullName = query.FullName.Trim().ToUpperInvariant();

            searchQuery = searchQuery.Where(user =>
                ((user.FirstName + " " +
                  (user.MiddleName ?? string.Empty) + " " +
                  user.LastName + " " +
                  (user.SecondLastName ?? string.Empty))
                .ToUpper())
                .Contains(normalizedFullName));
        }

        if (!string.IsNullOrWhiteSpace(query.DocumentNumber))
        {
            var normalizedDocumentNumber = query.DocumentNumber.Trim().ToUpperInvariant();
            searchQuery = searchQuery.Where(user => user.DocumentNumber.ToUpper().Contains(normalizedDocumentNumber));
        }

        if (query.AccountEnabled.HasValue)
        {
            searchQuery = searchQuery.Where(user => user.AccountEnabled == query.AccountEnabled.Value);
        }

        var rows = await searchQuery
            .OrderBy(user => user.FirstName)
            .ThenBy(user => user.LastName)
            .ThenBy(user => user.Username)
            .Select(user => new UserSearchResult
            {
                UserId = user.Id,
                CompanyId = user.CompanyId,
                Email = user.Email,
                Username = user.Username,
                RoleCode = user.Role.Code,
                AccountEnabled = user.AccountEnabled,
                FirstName = user.FirstName,
                MiddleName = user.MiddleName,
                LastName = user.LastName,
                SecondLastName = user.SecondLastName,
                DocumentNumber = user.DocumentNumber
            })
            .ToListAsync(cancellationToken);

        return rows;
    }
}
