using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Dtos;

public sealed class EnableUserPasswordRecoveryRequest
{
    public string Reason { get; init; } = string.Empty;
}
