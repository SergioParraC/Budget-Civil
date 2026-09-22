using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Users.Auth.Dtos;

public sealed class RegisterUserResponse
{
    [JsonPropertyName("user_id")]
    public Guid UserId { get; init; }

    [JsonPropertyName("rol_asignado")]
    public string AssignedRole { get; init; } = string.Empty;

    [JsonPropertyName("company_id")]
    public Guid? CompanyId { get; init; }

    [JsonPropertyName("estado_cuenta")]
    public bool AccountEnabled { get; init; }

    [JsonPropertyName("fecha_creacion")]
    public DateTime CreatedAtUtc { get; init; }
}
