using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Users.Administration.Dtos;

public sealed class UserResidenceRequest
{
    [JsonPropertyName("municipio")]
    public string City { get; init; } = string.Empty;

    [JsonPropertyName("departamento")]
    public string Department { get; init; } = string.Empty;

    [JsonPropertyName("pais")]
    public string Country { get; init; } = string.Empty;
}
