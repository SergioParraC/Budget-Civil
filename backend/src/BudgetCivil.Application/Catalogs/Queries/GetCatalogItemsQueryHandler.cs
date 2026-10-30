using System.Text.Json.Serialization;
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

public sealed class GetCatalogItemsQueryHandler(ListingQueryPipeline listingQueryPipeline)
{
    private static readonly CatalogCountry[] Countries =
    [
        new(Guid.Parse("85a4f3d6-9c28-4e4d-9a36-cf165e887919"), "CO", "Colombia"),
        new(Guid.Parse("45d88ed2-6943-4e5c-9c8d-6036f2f90f5e"), "PE", "Peru"),
        new(Guid.Parse("7048b712-7793-4320-a56b-43eb2fd7688d"), "CL", "Chile")
    ];

    private static readonly CatalogDepartment[] Departments =
    [
        new(Guid.Parse("8fa8b466-f3ea-4491-a2dd-8eb79fba399e"), Countries[0].Id, "CUN", "Cundinamarca"),
        new(Guid.Parse("d2979306-cf4a-42f3-97f7-e4be359b6e24"), Countries[0].Id, "ANT", "Antioquia"),
        new(Guid.Parse("8474cd90-5fa3-4cc6-ab7c-c191bcc22403"), Countries[1].Id, "LIM", "Lima")
    ];

    private static readonly CatalogMunicipality[] Municipalities =
    [
        new(Guid.Parse("3b23ed9c-db6a-4d1e-a4d0-cfdd2375c523"), Departments[0].Id, "BOG", "Bogota"),
        new(Guid.Parse("f7eb7da9-3d64-4824-ad33-d14e4fdd1175"), Departments[0].Id, "SOA", "Soacha"),
        new(Guid.Parse("4d915316-6369-443f-acfc-d9815c68e6cc"), Departments[1].Id, "MED", "Medellin"),
        new(Guid.Parse("7b9c1295-3218-4961-96c8-f67ece88707d"), Departments[2].Id, "LIM", "Lima")
    ];

    private static readonly CatalogCurrency[] Currencies =
    [
        new(Guid.Parse("f28ea3f1-90fd-4c52-8d7d-8b7fedf51520"), "COP", "Peso Colombiano"),
        new(Guid.Parse("79a6afef-c74d-4126-b039-a0cf6435caf4"), "USD", "Dolar Estadounidense"),
        new(Guid.Parse("94393a3a-fe6e-4c02-ae6d-3ec74666ce16"), "EUR", "Euro")
    ];

    private static readonly CatalogDocumentType[] DocumentTypes =
    [
        new(Guid.Parse("cd8f8756-402a-4c42-9402-9efdf5d37936"), "CC", "Cedula de ciudadania"),
        new(Guid.Parse("62d1fd51-c527-45a4-95d6-c8075d1feb0a"), "CE", "Cedula de extranjeria"),
        new(Guid.Parse("250a4f5f-68c2-4ccf-b8ca-df239f4f18e0"), "NIT", "Numero de identificacion tributaria")
    ];

    public async Task<CatalogListingResponse<CountryCatalogItem>> GetCountriesAsync(
        CatalogListingQuery query,
        CancellationToken cancellationToken = default)
    {
        var pagination = await listingQueryPipeline.ValidateAndNormalizeAsync(query.Page, query.PageSize, cancellationToken);

        var filtered = Countries
            .Where(country => MatchesSearch(country.Code, country.Name, query.Search))
            .OrderBy(country => country.Name)
            .ToArray();

        return new CatalogListingResponse<CountryCatalogItem>
        {
            Items = filtered
                .Skip((pagination.Page - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .Select(country => new CountryCatalogItem
                {
                    Id = country.Id,
                    Code = country.Code,
                    Name = country.Name
                })
                .ToArray(),
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            TotalItems = filtered.Length
        };
    }

    public async Task<CatalogListingResponse<DepartmentCatalogItem>> GetDepartmentsAsync(
        DepartmentCatalogListingQuery query,
        CancellationToken cancellationToken = default)
    {
        var pagination = await listingQueryPipeline.ValidateAndNormalizeAsync(query.Page, query.PageSize, cancellationToken);

        var filtered = Departments
            .Where(department =>
                (!query.CountryId.HasValue || department.CountryId == query.CountryId.Value) &&
                MatchesSearch(department.Code, department.Name, query.Search))
            .OrderBy(department => department.Name)
            .ToArray();

        return new CatalogListingResponse<DepartmentCatalogItem>
        {
            Items = filtered
                .Skip((pagination.Page - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
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
            TotalItems = filtered.Length
        };
    }

    public async Task<CatalogListingResponse<MunicipalityCatalogItem>> GetMunicipalitiesAsync(
        MunicipalityCatalogListingQuery query,
        CancellationToken cancellationToken = default)
    {
        var pagination = await listingQueryPipeline.ValidateAndNormalizeAsync(query.Page, query.PageSize, cancellationToken);

        var filtered = Municipalities
            .Where(municipality =>
                (!query.DepartmentId.HasValue || municipality.DepartmentId == query.DepartmentId.Value) &&
                MatchesSearch(municipality.Code, municipality.Name, query.Search))
            .OrderBy(municipality => municipality.Name)
            .ToArray();

        return new CatalogListingResponse<MunicipalityCatalogItem>
        {
            Items = filtered
                .Skip((pagination.Page - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
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
            TotalItems = filtered.Length
        };
    }

    public async Task<CatalogListingResponse<CurrencyCatalogItem>> GetCurrenciesAsync(
        CatalogListingQuery query,
        CancellationToken cancellationToken = default)
    {
        var pagination = await listingQueryPipeline.ValidateAndNormalizeAsync(query.Page, query.PageSize, cancellationToken);

        var filtered = Currencies
            .Where(currency => MatchesSearch(currency.IsoCode, currency.Name, query.Search))
            .OrderBy(currency => currency.Name)
            .ToArray();

        return new CatalogListingResponse<CurrencyCatalogItem>
        {
            Items = filtered
                .Skip((pagination.Page - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .Select(currency => new CurrencyCatalogItem
                {
                    Id = currency.Id,
                    IsoCode = currency.IsoCode,
                    Name = currency.Name
                })
                .ToArray(),
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            TotalItems = filtered.Length
        };
    }

    public async Task<CatalogListingResponse<DocumentTypeCatalogItem>> GetDocumentTypesAsync(
        CatalogListingQuery query,
        CancellationToken cancellationToken = default)
    {
        var pagination = await listingQueryPipeline.ValidateAndNormalizeAsync(query.Page, query.PageSize, cancellationToken);

        var filtered = DocumentTypes
            .Where(documentType => MatchesSearch(documentType.Code, documentType.Name, query.Search))
            .OrderBy(documentType => documentType.Name)
            .ToArray();

        return new CatalogListingResponse<DocumentTypeCatalogItem>
        {
            Items = filtered
                .Skip((pagination.Page - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .Select(documentType => new DocumentTypeCatalogItem
                {
                    Id = documentType.Id,
                    Code = documentType.Code,
                    Name = documentType.Name
                })
                .ToArray(),
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            TotalItems = filtered.Length
        };
    }

    private static bool MatchesSearch(string code, string name, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return true;
        }

        var normalizedSearch = search.Trim();

        return code.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
               name.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record CatalogCountry(Guid Id, string Code, string Name);

    private sealed record CatalogDepartment(Guid Id, Guid CountryId, string Code, string Name);

    private sealed record CatalogMunicipality(Guid Id, Guid DepartmentId, string Code, string Name);

    private sealed record CatalogCurrency(Guid Id, string IsoCode, string Name);

    private sealed record CatalogDocumentType(Guid Id, string Code, string Name);
}
