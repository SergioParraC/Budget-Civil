using BudgetCivil.Application.Projects.Repositories;
using BudgetCivil.Domain.Projects;
using BudgetCivil.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Infrastructure.Persistence.Repositories;

public sealed class ProjectRepository(BudgetCivilDbContext dbContext)
    : BaseFeature002Repository(dbContext), IProjectRepository
{
    public Task<UserAccount?> GetActorByIdAsync(Guid actorUserId, CancellationToken cancellationToken = default)
    {
        return DbContext.UserAccounts
            .Include(user => user.Role)
            .SingleOrDefaultAsync(user => user.Id == actorUserId, cancellationToken);
    }

    public Task<Project?> GetProjectByIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return DbContext.Projects
            .Include(project => project.Configuration)
            .SingleOrDefaultAsync(project => project.Id == projectId, cancellationToken);
    }

    public Task<Company?> GetCompanyByIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return DbContext.Companies.SingleOrDefaultAsync(company => company.Id == companyId, cancellationToken);
    }

    public Task<bool> HasActiveDirectorInCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return DbContext.UserAccounts.AnyAsync(
            user =>
                user.CompanyId == companyId &&
                user.AccountEnabled &&
                !user.AccountLocked &&
                user.Role.IsActive &&
                user.Role.Code == ProjectStatusTransitionPolicy.DirectorRoleCode,
            cancellationToken);
    }

    public async Task<Guid?> GetOldestActiveDirectorUserIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbContext.UserAccounts
            .Where(user =>
                user.CompanyId == companyId &&
                user.AccountEnabled &&
                !user.AccountLocked &&
                user.Role.IsActive &&
                user.Role.Code == ProjectStatusTransitionPolicy.DirectorRoleCode)
            .OrderBy(user => user.CreatedAtUtc)
            .Select(user => (Guid?)user.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> AreProjectCatalogReferencesValidAsync(
        Guid countryId,
        Guid departmentId,
        Guid municipalityId,
        Guid baseCurrencyId,
        CancellationToken cancellationToken = default)
    {
        var countryExists = await DbContext.Countries.AnyAsync(
            country => country.Id == countryId && country.IsActive,
            cancellationToken);

        if (!countryExists)
        {
            return false;
        }

        var departmentExists = await DbContext.Departments.AnyAsync(
            department =>
                department.Id == departmentId &&
                department.CountryId == countryId &&
                department.IsActive,
            cancellationToken);

        if (!departmentExists)
        {
            return false;
        }

        var municipalityExists = await DbContext.Municipalities.AnyAsync(
            municipality =>
                municipality.Id == municipalityId &&
                municipality.DepartmentId == departmentId &&
                municipality.IsActive,
            cancellationToken);

        if (!municipalityExists)
        {
            return false;
        }

        return await DbContext.BaseCurrencies.AnyAsync(
            baseCurrency => baseCurrency.Id == baseCurrencyId && baseCurrency.IsActive,
            cancellationToken);
    }

    public Task AddProjectAsync(Project project, CancellationToken cancellationToken = default)
    {
        DbContext.Projects.Add(project);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<Project>> GetProjectsAsync(
        Guid? companyId,
        ProjectStatus? status,
        string? search,
        int pageNumber,
        int pageSize,
        bool includeInhabilitado,
        CancellationToken cancellationToken = default)
    {
        var query = BuildProjectsQuery(companyId, status, search, includeInhabilitado);

        return await query
            .OrderBy(project => project.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
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

    public new Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Project> BuildProjectsQuery(
        Guid? companyId,
        ProjectStatus? status,
        string? search,
        bool includeInhabilitado)
    {
        var query = DbContext.Projects.AsQueryable();

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

        return query.AsNoTracking();
    }
}
