namespace BudgetCivil.Api.Auth;

public static class RateLimitPolicies
{
    /// <summary>REQ-IDENTIDAD-06 / D-80: intentos de autorregistro por dirección IP.</summary>
    public const string Registration = "registration";
}
