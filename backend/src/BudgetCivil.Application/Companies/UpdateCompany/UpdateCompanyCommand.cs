using BudgetCivil.Application.Companies.Dtos;
using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Companies.UpdateCompany;

public sealed class UpdateCompanyCommand
{
    [JsonPropertyName("business_name")]
    public string BusinessName { get; init; } = string.Empty;

    [JsonPropertyName("tax_id")]
    public string TaxId { get; init; } = string.Empty;

    [JsonPropertyName("company_type")]
    public string? CompanyType { get; init; }

    [JsonPropertyName("territorial_level")]
    public string? TerritorialLevel { get; init; }

    [JsonPropertyName("representative")]
    public LegalRepresentativePayload? Representative { get; init; }
}
