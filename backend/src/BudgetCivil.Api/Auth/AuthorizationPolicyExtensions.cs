using Microsoft.AspNetCore.Authorization;

namespace BudgetCivil.Api.Auth;

public static class AuthorizationPolicyExtensions
{
    public static AuthorizationPolicyBuilder RequirePermission(this AuthorizationPolicyBuilder builder, string permissionKey)
    {
        return builder.AddRequirements(new PermissionRequirement(permissionKey));
    }
}
