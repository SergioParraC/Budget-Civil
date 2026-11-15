using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Suppliers.Repositories;
using BudgetCivil.Domain.Suppliers;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Infrastructure.Persistence.Repositories;

public sealed class SupplierRepository(BudgetCivilDbContext dbContext) : ISupplierRepository
{
    private BudgetCivilDbContext DbContext { get; } = dbContext;

    public Task<Supplier?> GetByIdAsync(Guid supplierId, CancellationToken cancellationToken = default)
    {
        return DbContext.Suppliers
            .Include(supplier => supplier.Country)
            .Include(supplier => supplier.Department)
            .Include(supplier => supplier.Municipality)
            .SingleOrDefaultAsync(supplier => supplier.Id == supplierId, cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid supplierId, CancellationToken cancellationToken = default)
    {
        return DbContext.Suppliers.AnyAsync(supplier => supplier.Id == supplierId, cancellationToken);
    }

    public Task<bool> BelongsToCompanyAsync(
        Guid supplierId,
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        return DbContext.Suppliers.AnyAsync(
            supplier => supplier.Id == supplierId && supplier.CompanyId == companyId,
            cancellationToken);
    }

    public async Task<LocationHierarchy> GetLocationHierarchyAsync(
        Guid? countryId,
        Guid? departmentId,
        Guid? municipalityId,
        CancellationToken cancellationToken = default)
    {
        bool countryExists = !countryId.HasValue || await DbContext.Countries
            .AnyAsync(country => country.Id == countryId.Value && country.IsActive, cancellationToken);

        Guid? departmentCountryId = null;
        bool departmentExists = true;
        if (departmentId.HasValue)
        {
            departmentCountryId = await DbContext.Departments
                .Where(department => department.Id == departmentId.Value && department.IsActive)
                .Select(department => (Guid?)department.CountryId)
                .SingleOrDefaultAsync(cancellationToken);

            departmentExists = departmentCountryId.HasValue;
        }

        Guid? municipalityDepartmentId = null;
        bool municipalityExists = true;
        if (municipalityId.HasValue)
        {
            municipalityDepartmentId = await DbContext.Municipalities
                .Where(municipality => municipality.Id == municipalityId.Value && municipality.IsActive)
                .Select(municipality => (Guid?)municipality.DepartmentId)
                .SingleOrDefaultAsync(cancellationToken);

            municipalityExists = municipalityDepartmentId.HasValue;
        }

        return new LocationHierarchy(
            departmentCountryId,
            municipalityDepartmentId,
            countryExists,
            departmentExists,
            municipalityExists);
    }

    public Task<SupplierLocation?> GetProjectLocationAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        // `Project` expone la ubicación por identificadores; los nombres se resuelven en el catálogo.
        return (from project in DbContext.Projects.AsNoTracking()
                where project.Id == projectId
                join country in DbContext.Countries on project.CountryId equals country.Id
                join department in DbContext.Departments on project.DepartmentId equals department.Id
                join municipality in DbContext.Municipalities on project.MunicipalityId equals municipality.Id
                select new SupplierLocation(
                    project.CountryId,
                    country.Name,
                    project.DepartmentId,
                    department.Name,
                    project.MunicipalityId,
                    municipality.Name))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        DbContext.Suppliers.Add(supplier);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await DbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictAppException(
                "Supplier was modified by another user.",
                "SUPPLIER_CONCURRENCY_CONFLICT");
        }
    }

    public async Task<IReadOnlyList<Supplier>> ListAsync(
        SupplierFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await BuildFilterQuery(filter)
            .OrderBy(supplier => supplier.BusinessName)
            .ThenBy(supplier => supplier.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(SupplierFilter filter, CancellationToken cancellationToken = default)
    {
        return BuildFilterQuery(filter).CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Supplier>> ListActiveOptionsAsync(
        SupplierFilter filter,
        int limit,
        CancellationToken cancellationToken = default)
    {
        return await BuildFilterQuery(filter)
            .Where(supplier => supplier.IsActive)
            .OrderBy(supplier => supplier.BusinessName)
            .ThenBy(supplier => supplier.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    private IQueryable<Supplier> BuildFilterQuery(SupplierFilter filter)
    {
        var query = DbContext.Suppliers
            .AsNoTracking()
            .Include(supplier => supplier.Country)
            .Include(supplier => supplier.Department)
            .Include(supplier => supplier.Municipality)
            .AsQueryable();

        if (filter.CompanyId.HasValue)
        {
            query = query.Where(supplier => supplier.CompanyId == filter.CompanyId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.BusinessName))
        {
            var businessName = filter.BusinessName.Trim();
            query = query.Where(supplier => supplier.BusinessName.Contains(businessName));
        }

        if (!string.IsNullOrWhiteSpace(filter.ContactName))
        {
            var contactName = filter.ContactName.Trim();
            query = query.Where(supplier =>
                supplier.ContactName != null && supplier.ContactName.Contains(contactName));
        }

        if (filter.CountryId.HasValue)
        {
            query = query.Where(supplier => supplier.CountryId == filter.CountryId.Value);
        }

        if (filter.DepartmentId.HasValue)
        {
            query = query.Where(supplier => supplier.DepartmentId == filter.DepartmentId.Value);
        }

        if (filter.MunicipalityId.HasValue)
        {
            query = query.Where(supplier => supplier.MunicipalityId == filter.MunicipalityId.Value);
        }

        return query;
    }
}
