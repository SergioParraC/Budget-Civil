using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Users.Search;

public sealed class UserSearchResult
{
    [JsonPropertyName("user_id")]
    public Guid UserId { get; init; }

    [JsonPropertyName("company_id")]
    public Guid? CompanyId { get; init; }

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; init; } = string.Empty;

    [JsonPropertyName("role_code")]
    public string RoleCode { get; init; } = string.Empty;

    [JsonPropertyName("estado_cuenta")]
    public bool AccountEnabled { get; init; }

    [JsonPropertyName("primer_nombre")]
    public string FirstName { get; init; } = string.Empty;

    [JsonPropertyName("segundo_nombre")]
    public string? MiddleName { get; init; }

    [JsonPropertyName("primer_apellido")]
    public string LastName { get; init; } = string.Empty;

    [JsonPropertyName("segundo_apellido")]
    public string? SecondLastName { get; init; }

    [JsonPropertyName("numero_documento")]
    public string DocumentNumber { get; init; } = string.Empty;
}
