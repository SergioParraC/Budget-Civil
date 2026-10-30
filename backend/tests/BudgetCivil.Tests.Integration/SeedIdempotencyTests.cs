using BudgetCivil.Infrastructure.Persistence;
using BudgetCivil.Infrastructure.Security;
using BudgetCivil.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BudgetCivil.Tests.Integration;

/// <summary>T-I-23: el `seed` de seguridad es idempotente y versionado (`D-82`).</summary>
public sealed class SeedIdempotencyTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task Ejecutar_el_seed_dos_veces_deja_el_mismo_inventario()
    {
        await using var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);

        var before = await CountAsync(context);

        var seeder = new SecurityBaselineSeeder(context, NullLogger<SecurityBaselineSeeder>.Instance);
        await seeder.SeedAsync();

        var after = await CountAsync(context);

        Assert.Equal(before, after);
        Assert.Equal(4, after.Roles);
        Assert.True(after.Permissions >= 27);
        Assert.All(
            await context.Roles.Select(role => role.Code).ToListAsync(),
            code => Assert.Contains(code, new[] { "ADMINISTRADOR", "DIRECTOR", "PRESUPUESTADOR", "AUDITOR" }));
    }

    [Fact]
    public async Task Los_cuatro_roles_base_tienen_los_permisos_de_la_matriz()
    {
        await using var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);

        var directorPermissions = await context.RolePermissions
            .Where(rolePermission => rolePermission.Role.Code == "DIRECTOR" && rolePermission.IsActive)
            .Select(rolePermission => rolePermission.Permission.PermissionKey)
            .ToListAsync();

        var administratorPermissions = await context.RolePermissions
            .Where(rolePermission => rolePermission.Role.Code == "ADMINISTRADOR" && rolePermission.IsActive)
            .Select(rolePermission => rolePermission.Permission.PermissionKey)
            .ToListAsync();

        var estimatorPermissions = await context.RolePermissions
            .Where(rolePermission => rolePermission.Role.Code == "PRESUPUESTADOR" && rolePermission.IsActive)
            .Select(rolePermission => rolePermission.Permission.PermissionKey)
            .ToListAsync();

        // DEC-22: el cambio de estado lo siembran Director y Presupuestador.
        Assert.Contains("PROJECTS_STATUS_CHANGE", directorPermissions);
        Assert.Contains("PROJECTS_STATUS_CHANGE", estimatorPermissions);

        // DEC-21: el Director no edita ni desactiva usuarios.
        Assert.DoesNotContain("USERS_UPDATE", directorPermissions);
        Assert.DoesNotContain("USERS_DEACTIVATE", directorPermissions);

        // GAP-16: el Administrador solo lee proyectos.
        Assert.Contains("PROJECTS_READ", administratorPermissions);
        Assert.DoesNotContain("PROJECTS_CREATE", administratorPermissions);
        Assert.DoesNotContain("PROJECTS_UPDATE", administratorPermissions);

        // REQ-IDENTIDAD-10: el Presupuestador no lista usuarios.
        Assert.DoesNotContain("USERS_READ", estimatorPermissions);
    }

    private static async Task<(int Roles, int Permissions, int RolePermissions)> CountAsync(
        BudgetCivilDbContext context) =>
        (
            await context.Roles.CountAsync(),
            await context.Permissions.CountAsync(),
            await context.RolePermissions.CountAsync());
}
