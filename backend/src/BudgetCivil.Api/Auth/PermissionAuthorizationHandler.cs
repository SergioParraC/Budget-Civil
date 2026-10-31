using System.Security.Claims;
using BudgetCivil.Application.Common.Auditing;
using BudgetCivil.Application.Common.Security;
using Microsoft.AspNetCore.Authorization;

namespace BudgetCivil.Api.Auth;

public sealed class PermissionAuthorizationHandler(
    IActorAccessReader actorAccessReader,
    ISecurityAuditService securityAuditService,
    ILogger<PermissionAuthorizationHandler> logger) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var httpContext = ResolveHttpContext(context);
        var actorUserId = TryGetUserId(context.User);
        var requiredPermissionLabel = string.Join("|", requirement.PermissionKeys);

        if (context.User.Identity?.IsAuthenticated != true || actorUserId is null)
        {
            await AuditDeniedAsync(actorUserId, null, requiredPermissionLabel, "UNAUTHENTICATED_OR_INVALID_SUBJECT", httpContext, context);
            context.Fail();
            return;
        }

        try
        {
            var authorizationSnapshot = await actorAccessReader.GetSnapshotAsync(actorUserId.Value);

            var authorizationMetadata = new Dictionary<string, string>
            {
                ["actor_role_code"] = authorizationSnapshot?.RoleCode ?? "unknown",
                ["account_enabled"] = authorizationSnapshot?.AccountEnabled.ToString() ?? "unknown",
                ["account_locked"] = authorizationSnapshot?.AccountLocked.ToString() ?? "unknown",
                ["role_is_active"] = authorizationSnapshot?.RoleIsActive.ToString() ?? "unknown",
                ["has_required_permission"] = "unknown",
                ["permission_key_checked"] = requiredPermissionLabel,
                ["permission_match_mode"] = requirement.MatchMode.ToString(),
                ["required_permission_keys"] = requiredPermissionLabel
            };

            if (authorizationSnapshot is null)
            {
                await AuditDeniedAsync(actorUserId, null, requiredPermissionLabel, "USER_NOT_FOUND", httpContext, context);
                context.Fail();
                return;
            }

            var grantedPermissions = authorizationSnapshot.GrantedPermissionKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var hasRequiredPermission = requirement.MatchMode == PermissionMatchMode.All
                ? requirement.PermissionKeys.All(requiredPermissionKey => grantedPermissions.Contains(requiredPermissionKey))
                : requirement.PermissionKeys.Any(requiredPermissionKey => grantedPermissions.Contains(requiredPermissionKey));

            authorizationMetadata["has_required_permission"] = hasRequiredPermission.ToString();

            if (!authorizationSnapshot.AccountEnabled || authorizationSnapshot.AccountLocked)
            {
                await AuditDeniedAsync(
                    actorUserId,
                    authorizationSnapshot.CompanyId,
                    requiredPermissionLabel,
                    "ACCOUNT_DISABLED_OR_LOCKED",
                    httpContext,
                    context,
                    authorizationMetadata);

                context.Fail();
                return;
            }

            if (!authorizationSnapshot.RoleIsActive)
            {
                await AuditDeniedAsync(
                    actorUserId,
                    authorizationSnapshot.CompanyId,
                    requiredPermissionLabel,
                    "ROLE_INACTIVE",
                    httpContext,
                    context,
                    authorizationMetadata);

                context.Fail();
                return;
            }

            if (!hasRequiredPermission)
            {
                await AuditDeniedAsync(
                    actorUserId,
                    authorizationSnapshot.CompanyId,
                    requiredPermissionLabel,
                    "PERMISSION_INACTIVE_OR_MISSING",
                    httpContext,
                    context,
                    authorizationMetadata);

                context.Fail();
                return;
            }

            context.Succeed(requirement);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Permission authorization failed for key {PermissionKey}", requiredPermissionLabel);
            await AuditDeniedAsync(actorUserId, null, requiredPermissionLabel, "AUTHORIZATION_HANDLER_ERROR", httpContext, context);
            context.Fail();
        }
    }

    private static HttpContext? ResolveHttpContext(AuthorizationHandlerContext context)
    {
        return context.Resource switch
        {
            HttpContext directContext => directContext,
            Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext mvcContext => mvcContext.HttpContext,
            _ => null
        };
    }

    private static Guid? TryGetUserId(ClaimsPrincipal principal)
    {
        var userIdentifier = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");

        return Guid.TryParse(userIdentifier, out var parsedUserId) ? parsedUserId : null;
    }

    private async Task AuditDeniedAsync(
        Guid? actorUserId,
        Guid? companyId,
        string permissionKey,
        string reasonCode,
        HttpContext? httpContext,
        AuthorizationHandlerContext authorizationHandlerContext,
        IReadOnlyDictionary<string, string>? additionalMetadata = null)
    {
        var metadata = new Dictionary<string, string>
        {
            ["reason"] = reasonCode,
            ["resource_type"] = authorizationHandlerContext.Resource?.GetType().Name ?? "unknown"
        };

        if (additionalMetadata is not null)
        {
            foreach (var entry in additionalMetadata)
            {
                metadata[entry.Key] = entry.Value;
            }
        }

        if (httpContext is not null)
        {
            metadata["path"] = httpContext.Request.Path.ToString();
            metadata["method"] = httpContext.Request.Method;
            metadata["traceId"] = httpContext.TraceIdentifier;
        }

        await securityAuditService.WriteAuthorizationDeniedAsync(
            actorUserId,
            companyId,
            permissionKey,
            reasonCode,
            metadata);
    }
}
