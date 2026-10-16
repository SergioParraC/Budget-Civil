namespace BudgetCivil.Api.Middleware;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AllowWithoutCompanyContextAttribute : Attribute
{
}
