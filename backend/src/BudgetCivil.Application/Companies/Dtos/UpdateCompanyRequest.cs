using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Companies.Dtos;

public sealed class UpdateCompanyRequest
{
    [JsonPropertyName("business_name")]
    public string BusinessName { get; init; } = string.Empty;

    [JsonPropertyName("tax_id")]
    public string TaxId { get; init; } = string.Empty;

    [JsonPropertyName("is_active")]
    public bool? IsActive { get; init; }
}
