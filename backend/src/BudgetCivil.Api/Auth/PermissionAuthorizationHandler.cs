using System.Security.Claims;
using BudgetCivil.Application.Common.Auditing;
using BudgetCivil.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Api.Auth;

public sealed class PermissionAuthorizationHandler(
    BudgetCivilDbContext dbContext,
    ISecurityAuditService securityAuditService,
    ILogger<PermissionAuthorizationHandler> logger) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var httpContext = ResolveHttpContext(context);
        var actorUserId = TryGetUserId(context.User);

        if (context.User.Identity?.IsAuthenticated != true || actorUserId is null)
        {
            await AuditDeniedAsync(actorUserId, null, requirement.PermissionKey, "UNAUTHENTICATED_OR_INVALID_SUBJECT", httpContext, context);
            context.Fail();
            return;
        }

        try
        {
            var authorizationSnapshot = await dbContext.UserAccounts
                .AsNoTracking()
                .Where(user => user.Id == actorUserId.Value)
                .Select(user => new
                {
                    user.CompanyId,
                    RoleCode = user.Role.Code,
                    user.AccountEnabled,
                    user.AccountLocked,
                    RoleIsActive = user.Role.IsActive,
                    HasPermission = user.Role.RolePermissions.Any(rolePermission =>
                        rolePermission.IsActive &&
                        rolePermission.Permission.IsActive &&
                        rolePermission.Permission.PermissionKey == requirement.PermissionKey)
                })
                .SingleOrDefaultAsync();

            var authorizationMetadata = new Dictionary<string, string>
            {
                ["actor_role_code"] = authorizationSnapshot?.RoleCode ?? "unknown",
                ["account_enabled"] = authorizationSnapshot?.AccountEnabled.ToString() ?? "unknown",
                ["account_locked"] = authorizationSnapshot?.AccountLocked.ToString() ?? "unknown",
                ["role_is_active"] = authorizationSnapshot?.RoleIsActive.ToString() ?? "unknown",
                ["has_required_permission"] = authorizationSnapshot?.HasPermission.ToString() ?? "unknown",
                ["permission_key_checked"] = requirement.PermissionKey
            };

            if (authorizationSnapshot is null)
            {
                await AuditDeniedAsync(actorUserId, null, requirement.PermissionKey, "USER_NOT_FOUND", httpContext, context);
                context.Fail();
                return;
            }

            if (!authorizationSnapshot.AccountEnabled || authorizationSnapshot.AccountLocked)
            {
                await AuditDeniedAsync(
                    actorUserId,
                    authorizationSnapshot.CompanyId,
                    requirement.PermissionKey,
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
                    requirement.PermissionKey,
                    "ROLE_INACTIVE",
                    httpContext,
                    context,
                    authorizationMetadata);
                context.Fail();
                return;
            }

            if (!authorizationSnapshot.HasPermission)
            {
                await AuditDeniedAsync(
                    actorUserId,
                    authorizationSnapshot.CompanyId,
                    requirement.PermissionKey,
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
            logger.LogWarning(exception, "Permission authorization failed for key {PermissionKey}", requirement.PermissionKey);
            await AuditDeniedAsync(actorUserId, null, requirement.PermissionKey, "AUTHORIZATION_HANDLER_ERROR", httpContext, context);
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
            metadata["trace_id"] = httpContext.TraceIdentifier;
        }

        await securityAuditService.WriteAuthorizationDeniedAsync(
            actorUserId,
            companyId,
            permissionKey,
            reasonCode,
            metadata);
    }
}
