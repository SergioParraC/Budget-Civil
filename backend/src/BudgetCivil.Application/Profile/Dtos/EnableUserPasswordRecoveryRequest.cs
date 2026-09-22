using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Profile.Dtos;

public sealed class EnableUserPasswordRecoveryRequest
{
    [JsonPropertyName("reason")]
    public string Reason { get; init; } = string.Empty;
}
