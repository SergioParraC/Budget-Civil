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

    [JsonPropertyName("is_active")]
    public bool IsActive { get; init; }
}
