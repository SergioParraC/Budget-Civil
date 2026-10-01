using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Companies.CreateCompany;

public sealed class CreateCompanyCommand
{
    [JsonPropertyName("business_name")]
    public string BusinessName { get; init; } = string.Empty;

    [JsonPropertyName("tax_id")]
    public string TaxId { get; init; } = string.Empty;

    [JsonPropertyName("is_active")]
    public bool? IsActive { get; init; }

    [JsonPropertyName("company_type")]
    public string? CompanyType { get; init; }

    [JsonPropertyName("territorial_level")]
    public string? TerritorialLevel { get; init; }

    [JsonPropertyName("representative")]
    public LegalRepresentativePayload? Representative { get; init; }
}

public sealed class LegalRepresentativePayload
{
    [JsonPropertyName("mode")]
    public string? Mode { get; init; }

    [JsonPropertyName("user_id")]
    public Guid? UserId { get; init; }

    [JsonPropertyName("full_name")]
    public string? FullName { get; init; }

    [JsonPropertyName("document_number")]
    public string? DocumentNumber { get; init; }
}
