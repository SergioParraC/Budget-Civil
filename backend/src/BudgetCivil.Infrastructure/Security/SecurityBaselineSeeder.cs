using BudgetCivil.Domain.Users.Entities;
using BudgetCivil.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BudgetCivil.Infrastructure.Security;

public sealed class SecurityBaselineSeeder(
    BudgetCivilDbContext dbContext,
    ILogger<SecurityBaselineSeeder> logger) : ISecurityBaselineSeeder
{
    private const string SeedVersion = "v1";

    private static readonly string[] PermissionKeys =
    {
        "APU_PUBLIC_READ",
        "CATALOGS_READ",
        "USERS_CREATE",
        "USERS_UPDATE",
        "USERS_DEACTIVATE",
        "USERS_ROLE_CHANGE",
        "USERS_COMPANY_CHANGE",
        "USERS_READ",
        "COMPANIES_CREATE",
        "COMPANIES_UPDATE",
        "COMPANIES_DEACTIVATE",
        "COMPANIES_REACTIVATE",
        "COMPANIES_READ",
        "COMPANY_MEMBERS_ASSIGN",
        "COMPANY_MEMBERS_DEACTIVATE",
        "PROJECTS_CREATE",
        "PROJECTS_UPDATE",
        "PROJECTS_STATUS_CHANGE",
        "PROJECTS_VISIBILITY_CHANGE",
        "PROJECTS_READ",
        "PROJECT_MEMBERS_ASSIGN",
        "PROJECT_MEMBERS_DEACTIVATE",
        "PROFILE_READ_SELF",
        "PROFILE_UPDATE_SELF",
        "PROFILE_CHANGE_PASSWORD_SELF",
        "USERS_PASSWORD_RECOVERY_ENABLE",
        "AUDIT_BOOTSTRAP_READ"
    };

    private static readonly (string Code, string Name, bool IsBaseRole)[] Roles =
    {
        ("ADMINISTRADOR", "Administrador", true),
        ("DIRECTOR", "Director de proyectos", true),
        ("PRESUPUESTADOR", "Presupuestador", true),
        ("AUDITOR", "Auditor", true)
    };

    private static readonly Dictionary<string, string[]> RolePermissions = new()
    {
        ["ADMINISTRADOR"] =
        [
            "USERS_CREATE",
            "USERS_UPDATE",
            "USERS_DEACTIVATE",
            "USERS_ROLE_CHANGE",
            "USERS_COMPANY_CHANGE",
            "USERS_READ",
            "CATALOGS_READ",
            "COMPANIES_CREATE",
            "COMPANIES_UPDATE",
            "COMPANIES_DEACTIVATE",
            "COMPANIES_REACTIVATE",
            "COMPANIES_READ",
            "COMPANY_MEMBERS_ASSIGN",
            "COMPANY_MEMBERS_DEACTIVATE",
            "PROJECTS_READ",
            "USERS_PASSWORD_RECOVERY_ENABLE",
            "AUDIT_BOOTSTRAP_READ",
            "PROFILE_READ_SELF",
            "PROFILE_UPDATE_SELF",
            "PROFILE_CHANGE_PASSWORD_SELF"
        ],
        ["DIRECTOR"] =
        [
            "APU_PUBLIC_READ",
            "CATALOGS_READ",
            "USERS_ROLE_CHANGE",
            "USERS_COMPANY_CHANGE",
            "USERS_READ",
            "COMPANIES_CREATE",
            "COMPANIES_UPDATE",
            "COMPANY_MEMBERS_ASSIGN",
            "COMPANY_MEMBERS_DEACTIVATE",
            "PROJECTS_CREATE",
            "PROJECTS_UPDATE",
            "PROJECTS_STATUS_CHANGE",
            "PROJECTS_VISIBILITY_CHANGE",
            "PROJECTS_READ",
            "PROJECT_MEMBERS_ASSIGN",
            "PROJECT_MEMBERS_DEACTIVATE",
            "PROFILE_READ_SELF",
            "PROFILE_UPDATE_SELF",
            "PROFILE_CHANGE_PASSWORD_SELF"
        ],
        ["PRESUPUESTADOR"] =
        [
            "APU_PUBLIC_READ",
            "CATALOGS_READ",
            "PROJECTS_CREATE",
            "PROJECTS_UPDATE",
            "PROJECTS_STATUS_CHANGE",
            "PROJECTS_READ",
            "PROJECT_MEMBERS_ASSIGN",
            "PROJECT_MEMBERS_DEACTIVATE",
            "PROFILE_READ_SELF",
            "PROFILE_UPDATE_SELF",
            "PROFILE_CHANGE_PASSWORD_SELF"
        ],
        ["AUDITOR"] =
        [
            "APU_PUBLIC_READ",
            "CATALOGS_READ",
            "USERS_READ",
            "PROJECTS_READ",
            "PROFILE_READ_SELF",
            "PROFILE_UPDATE_SELF",
            "PROFILE_CHANGE_PASSWORD_SELF"
        ]
    };

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;

        foreach (var roleDefinition in Roles)
        {
            var role = await dbContext.Roles
                .SingleOrDefaultAsync(existingRole => existingRole.Code == roleDefinition.Code, cancellationToken);

            if (role is null)
            {
                role = new Role
                {
                    Id = Guid.NewGuid(),
                    Code = roleDefinition.Code,
                    Name = roleDefinition.Name,
                    IsActive = true,
                    IsBaseRole = roleDefinition.IsBaseRole,
                    CreatedAtUtc = utcNow,
                    UpdatedAtUtc = utcNow
                };

                dbContext.Roles.Add(role);
            }
            else
            {
                role.Name = roleDefinition.Name;
                role.IsActive = true;
                role.IsBaseRole = roleDefinition.IsBaseRole;
                role.UpdatedAtUtc = utcNow;
            }
        }

        foreach (var permissionKey in PermissionKeys)
        {
            var permission = await dbContext.Permissions
                .SingleOrDefaultAsync(existingPermission => existingPermission.PermissionKey == permissionKey, cancellationToken);

            if (permission is null)
            {
                permission = new Permission
                {
                    Id = Guid.NewGuid(),
                    PermissionKey = permissionKey,
                    Description = permissionKey.Replace('_', ' '),
                    IsActive = true,
                    SeedVersion = SeedVersion,
                    CreatedAtUtc = utcNow,
                    UpdatedAtUtc = utcNow
                };

                dbContext.Permissions.Add(permission);
            }
            else
            {
                permission.IsActive = true;
                permission.SeedVersion = SeedVersion;
                permission.UpdatedAtUtc = utcNow;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var rolesByCode = await dbContext.Roles
            .ToDictionaryAsync(role => role.Code, cancellationToken);

        var permissionsByKey = await dbContext.Permissions
            .ToDictionaryAsync(permission => permission.PermissionKey, cancellationToken);

        foreach (var rolePermissionDefinition in RolePermissions)
        {
            var role = rolesByCode[rolePermissionDefinition.Key];

            foreach (var permissionKey in rolePermissionDefinition.Value)
            {
                var permission = permissionsByKey[permissionKey];

                var rolePermission = await dbContext.RolePermissions.SingleOrDefaultAsync(
                    existingRolePermission =>
                        existingRolePermission.RoleId == role.Id &&
                        existingRolePermission.PermissionId == permission.Id,
                    cancellationToken);

                if (rolePermission is null)
                {
                    rolePermission = new RolePermission
                    {
                        Id = Guid.NewGuid(),
                        RoleId = role.Id,
                        PermissionId = permission.Id,
                        IsActive = true,
                        AssignedAtUtc = utcNow,
                        SeedVersion = SeedVersion
                    };

                    dbContext.RolePermissions.Add(rolePermission);
                }
                else
                {
                    rolePermission.IsActive = true;
                    rolePermission.SeedVersion = SeedVersion;
                    rolePermission.RevokedAtUtc = null;
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Security baseline seed completed with version {SeedVersion}", SeedVersion);
    }
}
