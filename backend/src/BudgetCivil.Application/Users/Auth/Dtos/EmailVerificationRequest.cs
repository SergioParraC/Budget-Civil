namespace BudgetCivil.Application.Users.Auth.Dtos;

public sealed class EmailVerificationRequest
{
    public string Token { get; init; } = string.Empty;
}
