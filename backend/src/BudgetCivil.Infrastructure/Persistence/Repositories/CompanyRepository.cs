using BudgetCivil.Application.Companies.Repositories;
using BudgetCivil.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Infrastructure.Persistence.Repositories;

public sealed class CompanyRepository(BudgetCivilDbContext dbContext) : ICompanyRepository
{
    public Task<UserAccount?> GetActorByIdAsync(Guid actorUserId, CancellationToken cancellationToken = default)
    {
        return dbContext.UserAccounts
            .Include(user => user.Role)
            .SingleOrDefaultAsync(user => user.Id == actorUserId, cancellationToken);
    }

    public Task<Company?> GetCompanyByIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return dbContext.Companies
            .SingleOrDefaultAsync(company => company.Id == companyId, cancellationToken);
    }

    public Task<bool> TaxIdExistsAsync(string taxId, Guid? excludingCompanyId = null, CancellationToken cancellationToken = default)
    {
        var normalizedTaxId = taxId.Trim().ToUpperInvariant();

        return dbContext.Companies.AnyAsync(
            company =>
                company.TaxId.ToUpper() == normalizedTaxId &&
                (!excludingCompanyId.HasValue || company.Id != excludingCompanyId.Value),
            cancellationToken);
    }

    public Task<UserCompanyMembership?> GetActiveMembershipByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return dbContext.UserCompanyMemberships
            .SingleOrDefaultAsync(membership => membership.UserId == userId && membership.IsActive, cancellationToken);
    }

    public Task AddCompanyAsync(Company company, CancellationToken cancellationToken = default)
    {
        dbContext.Companies.Add(company);
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
