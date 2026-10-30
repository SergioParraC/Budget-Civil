using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Companies.ChangeCompanyStatus;

public sealed class DisableCompanyCommand
{
    public string? Reason { get; init; }

    public string? LegacyReason { get; init; }

    public string GetNormalizedReason()
    {
        return !string.IsNullOrWhiteSpace(Reason)
            ? Reason.Trim()
            : LegacyReason?.Trim() ?? string.Empty;
    }
}
