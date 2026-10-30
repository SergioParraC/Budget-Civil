using BudgetCivil.Application.Listings.Repositories;
using BudgetCivil.Domain.Projects;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Infrastructure.Persistence.Repositories;

public sealed class ListingRepository(BudgetCivilDbContext dbContext)
    : BaseFeature002Repository(dbContext), IListingRepository
{
    public Task<ListingActorSnapshot?> GetActorSnapshotAsync(Guid actorUserId, CancellationToken cancellationToken = default)
    {
        return DbContext.UserAccounts
            .AsNoTracking()
            .Where(user => user.Id == actorUserId)
            .Select(user => new ListingActorSnapshot(
                user.Id,
                user.CompanyId,
                user.Role.Code,
                user.AccountEnabled,
                user.AccountLocked,
                user.Role.IsActive))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<bool> CompanyExistsAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return DbContext.Companies.AnyAsync(company => company.Id == companyId, cancellationToken);
    }

    public Task<int> CountUsersAsync(
        Guid? companyId,
        string? search,
        string? roleCode,
        bool? isActive,
        IReadOnlyCollection<string>? restrictedRoleCodes,
        CancellationToken cancellationToken = default)
    {
        return BuildUsersQuery(companyId, search, roleCode, isActive, restrictedRoleCodes).CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ListingUserRow>> GetUsersAsync(
        Guid? companyId,
        string? search,
        string? roleCode,
        bool? isActive,
        IReadOnlyCollection<string>? restrictedRoleCodes,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await BuildUsersQuery(companyId, search, roleCode, isActive, restrictedRoleCodes)
            .OrderBy(user => user.FirstName)
            .ThenBy(user => user.LastName)
            .ThenBy(user => user.Username)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(user => new ListingUserRow(
                user.Id,
                user.CompanyId,
                user.Email,
                user.Username,
                user.Role.Code,
                user.AccountEnabled,
                user.FirstName,
                user.MiddleName,
                user.LastName,
                user.SecondLastName,
                user.DocumentNumber))
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountProjectsAsync(
        Guid? companyId,
        ProjectStatus? status,
        string? search,
        bool includeInhabilitado,
        CancellationToken cancellationToken = default)
    {
        return BuildProjectsQuery(companyId, status, search, includeInhabilitado).CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ListingProjectRow>> GetProjectsAsync(
        Guid? companyId,
        ProjectStatus? status,
        string? search,
        int pageNumber,
        int pageSize,
        bool includeInhabilitado,
        CancellationToken cancellationToken = default)
    {
        return await BuildProjectsQuery(companyId, status, search, includeInhabilitado)
            .OrderBy(project => project.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(project => new ListingProjectRow(
                project.Id,
                project.CompanyId,
                project.Name,
                project.Status,
                project.IsPublic))
            .ToListAsync(cancellationToken);
    }

    private IQueryable<BudgetCivil.Domain.Users.Entities.UserAccount> BuildUsersQuery(
        Guid? companyId,
        string? search,
        string? roleCode,
        bool? isActive,
        IReadOnlyCollection<string>? restrictedRoleCodes)
    {
        var query = DbContext.UserAccounts
            .AsNoTracking()
            .Include(user => user.Role)
            .AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(user => user.CompanyId == companyId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToUpperInvariant();
            query = query.Where(user =>
                user.Email.ToUpper().Contains(normalizedSearch) ||
                user.Username.ToUpper().Contains(normalizedSearch) ||
                (
                 ((user.FirstName + " " +
                   (user.MiddleName ?? string.Empty) + " " +
                   user.LastName + " " +
                   (user.SecondLastName ?? string.Empty)).ToUpper().Contains(normalizedSearch) ||
                  user.DocumentNumber.ToUpper().Contains(normalizedSearch))));
        }

        if (!string.IsNullOrWhiteSpace(roleCode))
        {
            var normalizedRoleCode = roleCode.Trim().ToUpperInvariant();
            query = query.Where(user => user.Role.Code == normalizedRoleCode);
        }

        if (restrictedRoleCodes is { Count: > 0 })
        {
            var roleSet = restrictedRoleCodes
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim().ToUpperInvariant())
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            query = query.Where(user => roleSet.Contains(user.Role.Code));
        }

        if (isActive.HasValue)
        {
            query = query.Where(user => user.AccountEnabled == isActive.Value);
        }

        return query;
    }

    private IQueryable<Project> BuildProjectsQuery(
        Guid? companyId,
        ProjectStatus? status,
        string? search,
        bool includeInhabilitado)
    {
        var query = DbContext.Projects.AsNoTracking().AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(project => project.CompanyId == companyId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(project => project.Status == status.Value);
        }

        if (!includeInhabilitado)
        {
            query = query.Where(project => project.Status != ProjectStatus.Inhabilitado);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToUpperInvariant();
            query = query.Where(project => project.Name.ToUpper().Contains(normalizedSearch));
        }

        return query;
    }
}
