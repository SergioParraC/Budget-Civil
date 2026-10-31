using BudgetCivil.Application.Companies.Repositories;
using BudgetCivil.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Infrastructure.Persistence.Repositories;

public sealed class CompanyRepository(BudgetCivilDbContext dbContext)
    : BaseFeature002Repository(dbContext), ICompanyRepository
{
    public Task<UserAccount?> GetActorByIdAsync(Guid actorUserId, CancellationToken cancellationToken = default)
    {
        return DbContext.UserAccounts
            .Include(user => user.Role)
            .SingleOrDefaultAsync(user => user.Id == actorUserId, cancellationToken);
    }

    public Task<Company?> GetCompanyByIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return DbContext.Companies
            .SingleOrDefaultAsync(company => company.Id == companyId, cancellationToken);
    }

    public Task<bool> TaxIdExistsAsync(string taxId, Guid? excludingCompanyId = null, CancellationToken cancellationToken = default)
    {
        var normalizedTaxId = taxId.Trim().ToUpperInvariant();

        return DbContext.Companies.AnyAsync(
            company =>
                company.TaxId.ToUpper() == normalizedTaxId &&
                (!excludingCompanyId.HasValue || company.Id != excludingCompanyId.Value),
            cancellationToken);
    }

    public Task AddCompanyAsync(Company company, CancellationToken cancellationToken = default)
    {
        DbContext.Companies.Add(company);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<Company>> GetCompaniesAsync(
        string? search,
        bool? isActive,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = BuildCompaniesQuery(search, isActive);

        return await query
            .OrderBy(company => company.BusinessName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountCompaniesAsync(string? search, bool? isActive, CancellationToken cancellationToken = default)
    {
        return BuildCompaniesQuery(search, isActive).CountAsync(cancellationToken);
    }

    private IQueryable<Company> BuildCompaniesQuery(string? search, bool? isActive)
    {
        var query = DbContext.Companies.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToUpperInvariant();
            query = query.Where(company =>
                company.BusinessName.ToUpper().Contains(normalizedSearch) ||
                company.TaxId.ToUpper().Contains(normalizedSearch));
        }

        if (isActive.HasValue)
        {
            query = query.Where(company => company.IsActive == isActive.Value);
        }

        return query;
    }

    public new Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }
}
