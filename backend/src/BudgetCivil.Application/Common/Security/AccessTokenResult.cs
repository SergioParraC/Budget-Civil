namespace BudgetCivil.Application.Common.Security;

public sealed record AccessTokenResult(
    string AccessToken,
    DateTime ExpiresAtUtc,
    string TokenType = "Bearer");
