using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Users.Auth.Dtos;

public sealed class LoginResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; init; } = "Bearer";

    [JsonPropertyName("expires_at")]
    public DateTime ExpiresAtUtc { get; init; }

    [JsonPropertyName("user")]
    public LoginUserPayload User { get; init; } = null!;
}

public sealed class LoginUserPayload
{
    [JsonPropertyName("user_id")]
    public Guid UserId { get; init; }

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; init; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; init; } = string.Empty;

    [JsonPropertyName("company_id")]
    public Guid? CompanyId { get; init; }

    [JsonPropertyName("estado_cuenta")]
    public bool AccountEnabled { get; init; }
}
