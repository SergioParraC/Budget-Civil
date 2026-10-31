using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Users.Auth.Dtos;

public sealed class LoginRequest
{
    public string Identifier { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}
