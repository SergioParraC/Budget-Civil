namespace BudgetCivil.Application.Users.Auth.Dtos;

public sealed class RequestPasswordRecoveryRequest
{
    public string Email { get; init; } = string.Empty;
}

public sealed class ConfirmPasswordRecoveryRequest
{
    public string Token { get; init; } = string.Empty;

    public string NewPassword { get; init; } = string.Empty;
}