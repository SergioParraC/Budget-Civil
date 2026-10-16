using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Companies.Dtos;

public sealed class UpdateCompanyRequest
{
    [JsonPropertyName("business_name")]
    public string BusinessName { get; init; } = string.Empty;

    [JsonPropertyName("tax_id")]
    public string TaxId { get; init; } = string.Empty;

    [JsonPropertyName("company_type")]
    public string CompanyType { get; init; } = string.Empty;

    [JsonPropertyName("territorial_level")]
    public string? TerritorialLevel { get; init; }

    [JsonPropertyName("country_id")]
    public Guid CountryId { get; init; }

    [JsonPropertyName("department_id")]
    public Guid DepartmentId { get; init; }

    [JsonPropertyName("municipality_id")]
    public Guid MunicipalityId { get; init; }

    [JsonPropertyName("representative")]
    public LegalRepresentativePayload? Representative { get; init; }

    [JsonPropertyName("is_active")]
    public bool? IsActive { get; init; }
}
