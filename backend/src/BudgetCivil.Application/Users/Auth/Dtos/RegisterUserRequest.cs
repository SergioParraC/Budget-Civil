using System.Text.Json;
using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Users.Auth.Dtos;

public sealed class RegisterUserRequest
{
    [JsonPropertyName("primer_nombre")]
    public string FirstName { get; init; } = string.Empty;

    [JsonPropertyName("primer_apellido")]
    public string LastName { get; init; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; init; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; init; } = string.Empty;

    [JsonPropertyName("tipo_documento")]
    public string DocumentType { get; init; } = string.Empty;

    [JsonPropertyName("numero_documento")]
    public string DocumentNumber { get; init; } = string.Empty;

    [JsonPropertyName("country_id")]
    public Guid CountryId { get; init; }

    [JsonPropertyName("department_id")]
    public Guid DepartmentId { get; init; }

    [JsonPropertyName("municipality_id")]
    public Guid MunicipalityId { get; init; }

    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; init; }
}
