using BudgetCivil.Application.Catalogs.Repositories;
using BudgetCivil.Application.Listings.Queries;

namespace BudgetCivil.Application.Catalogs.Queries;

public class CatalogListingQuery
{
    public string? Search { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 10;
}

public sealed class DepartmentCatalogListingQuery : CatalogListingQuery
{
    public Guid? CountryId { get; init; }
}

public sealed class MunicipalityCatalogListingQuery : CatalogListingQuery
{
    public Guid? DepartmentId { get; init; }
}

public sealed class CatalogListingResponse<TItem>
{
    public IReadOnlyList<TItem> Items { get; init; } = [];

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalItems { get; init; }
}

public sealed class CountryCatalogItem
{
    public Guid Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;
}

public sealed class DepartmentCatalogItem
{
    public Guid Id { get; init; }

    public Guid CountryId { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;
}

public sealed class MunicipalityCatalogItem
{
    public Guid Id { get; init; }

    public Guid DepartmentId { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;
}

public sealed class CurrencyCatalogItem
{
    public Guid Id { get; init; }

    public string IsoCode { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;
}

public sealed class DocumentTypeCatalogItem
{
    public Guid Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;
}

/// <summary>
/// Catálogos leídos de la base de datos: `countries`, `departments`, `municipalities`,
/// `base_currencies` y `document_types`. Solo se devuelven registros activos.
/// </summary>
public sealed class GetCatalogItemsQueryHandler(
    IGeographicCatalogRepository catalogRepository,
    ListingQueryPipeline listingQueryPipeline)
{
    public async Task<CatalogListingResponse<CountryCatalogItem>> GetCountriesAsync(
        CatalogListingQuery query,
        CancellationToken cancellationToken = default)
    {
        var pagination = await ValidatePaginationAsync(query, cancellationToken);
        var (items, totalItems) = await catalogRepository.GetCountriesAsync(
            query.Search,
            (pagination.Page - 1) * pagination.PageSize,
            pagination.PageSize,
            cancellationToken);

        return new CatalogListingResponse<CountryCatalogItem>
        {
            Items = items
                .Select(country => new CountryCatalogItem
                {
                    Id = country.Id,
                    Code = country.Code,
                    Name = country.Name
                })
                .ToArray(),
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            TotalItems = totalItems
        };
    }

    public async Task<CatalogListingResponse<DepartmentCatalogItem>> GetDepartmentsAsync(
        DepartmentCatalogListingQuery query,
        CancellationToken cancellationToken = default)
    {
        var pagination = await ValidatePaginationAsync(query, cancellationToken);
        var (items, totalItems) = await catalogRepository.GetDepartmentsAsync(
            query.CountryId,
            query.Search,
            (pagination.Page - 1) * pagination.PageSize,
            pagination.PageSize,
            cancellationToken);

        return new CatalogListingResponse<DepartmentCatalogItem>
        {
            Items = items
                .Select(department => new DepartmentCatalogItem
                {
                    Id = department.Id,
                    CountryId = department.CountryId,
                    Code = department.Code,
                    Name = department.Name
                })
                .ToArray(),
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            TotalItems = totalItems
        };
    }

    public async Task<CatalogListingResponse<MunicipalityCatalogItem>> GetMunicipalitiesAsync(
        MunicipalityCatalogListingQuery query,
        CancellationToken cancellationToken = default)
    {
        var pagination = await ValidatePaginationAsync(query, cancellationToken);
        var (items, totalItems) = await catalogRepository.GetMunicipalitiesAsync(
            query.DepartmentId,
            query.Search,
            (pagination.Page - 1) * pagination.PageSize,
            pagination.PageSize,
            cancellationToken);

        return new CatalogListingResponse<MunicipalityCatalogItem>
        {
            Items = items
                .Select(municipality => new MunicipalityCatalogItem
                {
                    Id = municipality.Id,
                    DepartmentId = municipality.DepartmentId,
                    Code = municipality.Code,
                    Name = municipality.Name
                })
                .ToArray(),
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            TotalItems = totalItems
        };
    }

    public async Task<CatalogListingResponse<CurrencyCatalogItem>> GetCurrenciesAsync(
        CatalogListingQuery query,
        CancellationToken cancellationToken = default)
    {
        var pagination = await ValidatePaginationAsync(query, cancellationToken);
        var (items, totalItems) = await catalogRepository.GetBaseCurrenciesAsync(
            query.Search,
            (pagination.Page - 1) * pagination.PageSize,
            pagination.PageSize,
            cancellationToken);

        return new CatalogListingResponse<CurrencyCatalogItem>
        {
            Items = items
                .Select(baseCurrency => new CurrencyCatalogItem
                {
                    Id = baseCurrency.Id,
                    IsoCode = baseCurrency.IsoCode,
                    Name = baseCurrency.Name
                })
                .ToArray(),
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            TotalItems = totalItems
        };
    }

    public async Task<CatalogListingResponse<DocumentTypeCatalogItem>> GetDocumentTypesAsync(
        CatalogListingQuery query,
        CancellationToken cancellationToken = default)
    {
        var pagination = await ValidatePaginationAsync(query, cancellationToken);
        var (items, totalItems) = await catalogRepository.GetDocumentTypesAsync(
            query.Search,
            (pagination.Page - 1) * pagination.PageSize,
            pagination.PageSize,
            cancellationToken);

        return new CatalogListingResponse<DocumentTypeCatalogItem>
        {
            Items = items
                .Select(documentType => new DocumentTypeCatalogItem
                {
                    Id = documentType.Id,
                    Code = documentType.Code,
                    Name = documentType.Name
                })
                .ToArray(),
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            TotalItems = totalItems
        };
    }

    private Task<ListingPagination> ValidatePaginationAsync(
        CatalogListingQuery query,
        CancellationToken cancellationToken) =>
        listingQueryPipeline.ValidateAndNormalizeAsync(query.Page, query.PageSize, cancellationToken);
}
