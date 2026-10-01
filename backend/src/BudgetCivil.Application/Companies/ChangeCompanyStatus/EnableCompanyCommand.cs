using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Companies.ChangeCompanyStatus;

public sealed class EnableCompanyCommand
{
    [JsonPropertyName("reason")]
    public string? Reason { get; init; }
}
