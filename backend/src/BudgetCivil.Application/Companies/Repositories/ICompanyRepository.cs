using BudgetCivil.Domain.Users.Entities;

namespace BudgetCivil.Application.Companies.Repositories;

public interface ICompanyRepository
{
    Task<UserAccount?> GetActorByIdAsync(Guid actorUserId, CancellationToken cancellationToken = default);

    Task<Company?> GetCompanyByIdAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<bool> TaxIdExistsAsync(string taxId, Guid? excludingCompanyId = null, CancellationToken cancellationToken = default);

    Task<UserCompanyMembership?> GetActiveMembershipByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddCompanyAsync(Company company, CancellationToken cancellationToken = default);

    Task AddMembershipAsync(UserCompanyMembership membership, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Company>> GetCompaniesAsync(
        string? search,
        bool? isActive,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountCompaniesAsync(string? search, bool? isActive, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
