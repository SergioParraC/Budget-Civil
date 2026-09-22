using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Companies.Dtos;

public sealed class DeactivateCompanyRequest
{
    [JsonPropertyName("motivo")]
    public string Reason { get; init; } = string.Empty;
}
