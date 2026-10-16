using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Profile.Dtos;

public sealed class ChangeMyPasswordRequest
{
    [JsonPropertyName("current_password")]
    public string CurrentPassword { get; init; } = string.Empty;

    [JsonPropertyName("new_password")]
    public string NewPassword { get; init; } = string.Empty;
}
