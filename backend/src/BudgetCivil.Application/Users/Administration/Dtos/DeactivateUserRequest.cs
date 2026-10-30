using System.Text.Json;
using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Users.Administration.Dtos;

public sealed class DeactivateUserRequest
{
    public string Reason { get; init; } = string.Empty;

    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; init; }
}
