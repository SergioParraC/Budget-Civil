using Microsoft.AspNetCore.Authorization;

namespace BudgetCivil.Api.Auth;

public sealed class PermissionRequirement(string permissionKey) : IAuthorizationRequirement
{
    public string PermissionKey { get; } = permissionKey;
}
