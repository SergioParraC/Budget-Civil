using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Users.Auth.Dtos;

public sealed class LoginResponse
{
    public string AccessToken { get; init; } = string.Empty;

    public string TokenType { get; init; } = "Bearer";

    public DateTime ExpiresAtUtc { get; init; }

    public LoginUserPayload User { get; init; } = null!;
}

public sealed class LoginUserPayload
{
    public Guid UserId { get; init; }

    public string Email { get; init; } = string.Empty;

    public string Username { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;

    public Guid? CompanyId { get; init; }

    public bool AccountEnabled { get; init; }
}
