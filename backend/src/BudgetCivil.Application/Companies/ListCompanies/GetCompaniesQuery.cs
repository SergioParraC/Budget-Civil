using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Companies.ListCompanies;

public sealed class GetCompaniesQuery
{
    [JsonPropertyName("search")]
    public string? Search { get; init; }

    [JsonPropertyName("is_active")]
    public bool? IsActive { get; init; }

    [JsonPropertyName("page_number")]
    public int PageNumber { get; init; } = 1;

    [JsonPropertyName("page_size")]
    public int PageSize { get; init; } = 10;
}

public sealed class GetCompaniesResponse
{
    [JsonPropertyName("items")]
    public IReadOnlyList<CompanyListItem> Items { get; init; } = [];

    [JsonPropertyName("page_number")]
    public int PageNumber { get; init; }

    [JsonPropertyName("page_size")]
    public int PageSize { get; init; }

    [JsonPropertyName("total_items")]
    public int TotalItems { get; init; }
}

public sealed class CompanyListItem
{
    [JsonPropertyName("company_id")]
    public Guid CompanyId { get; init; }

    [JsonPropertyName("business_name")]
    public string BusinessName { get; init; } = string.Empty;

    [JsonPropertyName("tax_id")]
    public string TaxId { get; init; } = string.Empty;

    [JsonPropertyName("company_type")]
    public string? CompanyType { get; init; }

    [JsonPropertyName("territorial_level")]
    public string? TerritorialLevel { get; init; }

    [JsonPropertyName("country_id")]
    public Guid? CountryId { get; init; }

    [JsonPropertyName("department_id")]
    public Guid? DepartmentId { get; init; }

    [JsonPropertyName("municipality_id")]
    public Guid? MunicipalityId { get; init; }

    [JsonPropertyName("representative_mode")]
    public string? RepresentativeMode { get; init; }

    [JsonPropertyName("representative_user_id")]
    public Guid? RepresentativeUserId { get; init; }

    [JsonPropertyName("representative_full_name")]
    public string? RepresentativeFullName { get; init; }

    [JsonPropertyName("representative_document_type_id")]
    public Guid? RepresentativeDocumentTypeId { get; init; }

    [JsonPropertyName("representative_document_number")]
    public string? RepresentativeDocumentNumber { get; init; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; init; }

    [JsonPropertyName("created_by_user_id")]
    public Guid? CreatedByUserId { get; init; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAtUtc { get; init; }

    [JsonPropertyName("last_modified_by_user_id")]
    public Guid? LastModifiedByUserId { get; init; }

    [JsonPropertyName("last_modified_at")]
    public DateTime? LastModifiedAtUtc { get; init; }
}
