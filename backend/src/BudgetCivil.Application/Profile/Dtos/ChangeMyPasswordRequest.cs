using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Dtos;

public sealed class ChangeMyPasswordRequest
{
    public string CurrentPassword { get; init; } = string.Empty;

    public string NewPassword { get; init; } = string.Empty;
}
