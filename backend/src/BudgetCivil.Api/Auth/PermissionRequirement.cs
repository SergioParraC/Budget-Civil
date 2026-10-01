using Microsoft.AspNetCore.Authorization;

namespace BudgetCivil.Api.Auth;

public enum PermissionMatchMode
{
    Any = 1,
    All = 2
}

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(IEnumerable<string> permissionKeys, PermissionMatchMode matchMode = PermissionMatchMode.Any)
    {
        var normalizedPermissionKeys = permissionKeys
            .Where(permissionKey => !string.IsNullOrWhiteSpace(permissionKey))
            .Select(permissionKey => permissionKey.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalizedPermissionKeys.Length == 0)
        {
            throw new ArgumentException("At least one permission key is required.", nameof(permissionKeys));
        }

        PermissionKeys = normalizedPermissionKeys;
        MatchMode = matchMode;
    }

    public IReadOnlyList<string> PermissionKeys { get; }

    public PermissionMatchMode MatchMode { get; }

    public string PermissionKey => PermissionKeys[0];
}
