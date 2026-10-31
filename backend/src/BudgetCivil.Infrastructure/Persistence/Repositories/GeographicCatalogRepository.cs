using BudgetCivil.Application.Catalogs.Repositories;
using BudgetCivil.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Infrastructure.Persistence.Repositories;

public sealed class GeographicCatalogRepository(BudgetCivilDbContext dbContext) : IGeographicCatalogRepository
{
    public Task<bool> IsValidActiveLocationAsync(
        Guid countryId,
        Guid departmentId,
        Guid municipalityId,
        CancellationToken cancellationToken = default)
    {
        return (
            from municipality in dbContext.Municipalities
            join department in dbContext.Departments on municipality.DepartmentId equals department.Id
            join country in dbContext.Countries on department.CountryId equals country.Id
            where municipality.Id == municipalityId
                && municipality.IsActive
                && department.Id == departmentId
                && department.IsActive
                && country.Id == countryId
                && country.IsActive
            select municipality.Id)
            .AnyAsync(cancellationToken);
    }

    public Task<bool> IsActiveDocumentTypeAsync(Guid documentTypeId, CancellationToken cancellationToken = default)
    {
        return dbContext.DocumentTypes
            .AnyAsync(documentType => documentType.Id == documentTypeId && documentType.IsActive, cancellationToken);
    }

    public Task<bool> IsActiveUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return dbContext.UserAccounts
            .AnyAsync(user => user.Id == userId && user.AccountEnabled, cancellationToken);
    }

    public async Task<(IReadOnlyList<Country> Items, int Total)> GetCountriesAsync(
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = BuildCountriesQuery(search);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(country => country.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<(IReadOnlyList<Department> Items, int Total)> GetDepartmentsAsync(
        Guid? countryId,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = BuildDepartmentsQuery(countryId, search);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(department => department.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<(IReadOnlyList<Municipality> Items, int Total)> GetMunicipalitiesAsync(
        Guid? departmentId,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = BuildMunicipalitiesQuery(departmentId, search);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(municipality => municipality.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<(IReadOnlyList<BaseCurrency> Items, int Total)> GetBaseCurrenciesAsync(
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = BuildBaseCurrenciesQuery(search);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(baseCurrency => baseCurrency.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<(IReadOnlyList<DocumentType> Items, int Total)> GetDocumentTypesAsync(
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = BuildDocumentTypesQuery(search);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(documentType => documentType.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    private IQueryable<Country> BuildCountriesQuery(string? search)
    {
        var query = dbContext.Countries
            .AsNoTracking()
            .Where(country => country.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToUpperInvariant();
            query = query.Where(country =>
                country.Code.ToUpper().Contains(normalizedSearch) ||
                country.Name.ToUpper().Contains(normalizedSearch));
        }

        return query;
    }

    private IQueryable<Department> BuildDepartmentsQuery(Guid? countryId, string? search)
    {
        var query = dbContext.Departments
            .AsNoTracking()
            .Where(department => department.IsActive);

        if (countryId.HasValue)
        {
            query = query.Where(department => department.CountryId == countryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToUpperInvariant();
            query = query.Where(department =>
                department.Code.ToUpper().Contains(normalizedSearch) ||
                department.Name.ToUpper().Contains(normalizedSearch));
        }

        return query;
    }

    private IQueryable<Municipality> BuildMunicipalitiesQuery(Guid? departmentId, string? search)
    {
        var query = dbContext.Municipalities
            .AsNoTracking()
            .Where(municipality => municipality.IsActive);

        if (departmentId.HasValue)
        {
            query = query.Where(municipality => municipality.DepartmentId == departmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToUpperInvariant();
            query = query.Where(municipality =>
                municipality.Code.ToUpper().Contains(normalizedSearch) ||
                municipality.Name.ToUpper().Contains(normalizedSearch));
        }

        return query;
    }

    private IQueryable<BaseCurrency> BuildBaseCurrenciesQuery(string? search)
    {
        var query = dbContext.BaseCurrencies
            .AsNoTracking()
            .Where(baseCurrency => baseCurrency.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToUpperInvariant();
            query = query.Where(baseCurrency =>
                baseCurrency.IsoCode.ToUpper().Contains(normalizedSearch) ||
                baseCurrency.Name.ToUpper().Contains(normalizedSearch));
        }

        return query;
    }

    private IQueryable<DocumentType> BuildDocumentTypesQuery(string? search)
    {
        var query = dbContext.DocumentTypes
            .AsNoTracking()
            .Where(documentType => documentType.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToUpperInvariant();
            query = query.Where(documentType =>
                documentType.Code.ToUpper().Contains(normalizedSearch) ||
                documentType.Name.ToUpper().Contains(normalizedSearch));
        }

        return query;
    }
}
