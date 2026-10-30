using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Companies.ListCompanies;

public sealed class GetCompaniesQuery
{
    public string? Search { get; init; }

    public bool? IsActive { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 10;
}

public sealed class GetCompaniesResponse
{
    public IReadOnlyList<CompanyListItem> Items { get; init; } = [];

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalItems { get; init; }
}

public sealed class CompanyListItem
{
    public Guid CompanyId { get; init; }

    public string BusinessName { get; init; } = string.Empty;

    public string TaxId { get; init; } = string.Empty;

    public string? CompanyType { get; init; }

    public string? TerritorialLevel { get; init; }

    public Guid? CountryId { get; init; }

    public Guid? DepartmentId { get; init; }

    public Guid? MunicipalityId { get; init; }

    public string? RepresentativeMode { get; init; }

    public Guid? RepresentativeUserId { get; init; }

    public string? RepresentativeFullName { get; init; }

    public Guid? RepresentativeDocumentTypeId { get; init; }

    public string? RepresentativeDocumentNumber { get; init; }

    public bool IsActive { get; init; }

    public Guid? CreatedByUserId { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public Guid? LastModifiedByUserId { get; init; }

    public DateTime? LastModifiedAtUtc { get; init; }
}
