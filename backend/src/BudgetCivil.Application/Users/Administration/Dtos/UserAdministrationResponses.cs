using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Users.Administration.Dtos;

public sealed class CreateUserResponse
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

public sealed class UpdateUserResponse
{
    [JsonPropertyName("user_id")]
    public Guid UserId { get; init; }

    [JsonPropertyName("fecha_edicion")]
    public DateTime UpdatedAtUtc { get; init; }
}

public sealed class DeactivateUserResponse
{
    [JsonPropertyName("user_id")]
    public Guid UserId { get; init; }

    [JsonPropertyName("estado_cuenta")]
    public bool AccountEnabled { get; init; }
}

public sealed class ChangeUserRoleResponse
{
    [JsonPropertyName("user_id")]
    public Guid UserId { get; init; }

    [JsonPropertyName("rol_asignado")]
    public string AssignedRole { get; init; } = string.Empty;
}

public sealed class ChangeUserCompanyResponse
{
    [JsonPropertyName("user_id")]
    public Guid UserId { get; init; }

    [JsonPropertyName("company_id")]
    public Guid CompanyId { get; init; }
}
