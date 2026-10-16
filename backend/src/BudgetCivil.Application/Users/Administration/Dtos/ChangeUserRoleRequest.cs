using System.Text.Json;
using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Users.Administration.Dtos;

public sealed class ChangeUserRoleRequest
{
    [JsonPropertyName("rol_asignado")]
    public string AssignedRoleCode { get; init; } = string.Empty;

    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; init; }
}
