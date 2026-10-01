using Microsoft.AspNetCore.Authorization;

namespace BudgetCivil.Api.Auth;

public static class AuthorizationPolicyExtensions
{
    public static AuthorizationPolicyBuilder RequirePermission(this AuthorizationPolicyBuilder builder, string permissionKey)
    {
        return builder.RequireAnyPermission(permissionKey);
    }

    public static AuthorizationPolicyBuilder RequireAnyPermission(this AuthorizationPolicyBuilder builder, params string[] permissionKeys)
    {
        return builder.AddRequirements(new PermissionRequirement(permissionKeys, PermissionMatchMode.Any));
    }

    public static AuthorizationPolicyBuilder RequireAllPermissions(this AuthorizationPolicyBuilder builder, params string[] permissionKeys)
    {
        return builder.AddRequirements(new PermissionRequirement(permissionKeys, PermissionMatchMode.All));
    }
}
