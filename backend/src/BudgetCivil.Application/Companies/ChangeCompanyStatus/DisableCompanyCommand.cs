using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Companies.ChangeCompanyStatus;

public sealed class DisableCompanyCommand
{
    [JsonPropertyName("reason")]
    public string? Reason { get; init; }

    [JsonPropertyName("motivo")]
    public string? LegacyReason { get; init; }

    public string GetNormalizedReason()
    {
        return !string.IsNullOrWhiteSpace(Reason)
            ? Reason.Trim()
            : LegacyReason?.Trim() ?? string.Empty;
    }
}
