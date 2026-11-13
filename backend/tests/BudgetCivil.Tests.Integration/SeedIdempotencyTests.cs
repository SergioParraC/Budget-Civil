using BudgetCivil.Domain.Users.Entities;
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

        // REQ-IDENTIDAD-10 con D-85 y D-112: el Presupuestador busca usuarios de su compañía para
        // asignar Presupuestadores y Auditores a sus proyectos.
        Assert.Contains("USERS_READ", estimatorPermissions);

        // REQ-PROVEEDORES-07: Director y Presupuestador escriben; Administrador y Auditor solo leen.
        var auditorPermissions = await context.RolePermissions
            .Where(rolePermission => rolePermission.Role.Code == "AUDITOR" && rolePermission.IsActive)
            .Select(rolePermission => rolePermission.Permission.PermissionKey)
            .ToListAsync();

        Assert.Contains("SUPPLIERS_READ", directorPermissions);
        Assert.Contains("SUPPLIERS_WRITE", directorPermissions);
        Assert.Contains("SUPPLIERS_READ", estimatorPermissions);
        Assert.Contains("SUPPLIERS_WRITE", estimatorPermissions);
        Assert.Contains("SUPPLIERS_READ", administratorPermissions);
        Assert.DoesNotContain("SUPPLIERS_WRITE", administratorPermissions);
        Assert.Contains("SUPPLIERS_READ", auditorPermissions);
        Assert.DoesNotContain("SUPPLIERS_WRITE", auditorPermissions);
    }

    /// <summary>
    /// `REQ-IDENTIDAD-30`: la matriz es la fuente de verdad, así que una asignación que la base conservó de
    /// una versión anterior se revoca en el `seed` en lugar de permanecer concedida.
    /// </summary>
    [Fact]
    public async Task El_seed_revoca_las_asignaciones_que_ya_no_estan_en_la_matriz()
    {
        await using var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);

        var administrator = await context.Roles.SingleAsync(role => role.Code == "ADMINISTRADOR");
        var resourceWrite = await context.Permissions
            .SingleAsync(permission => permission.PermissionKey == "RESOURCES_WRITE");

        var existing = await context.RolePermissions.SingleOrDefaultAsync(rolePermission =>
            rolePermission.RoleId == administrator.Id && rolePermission.PermissionId == resourceWrite.Id);

        if (existing is not null)
        {
            // Ejecuciones repetidas de la suite: se vuelve a simular la deriva sobre la fila ya reconciliada.
            existing.IsActive = true;
            existing.RevokedAtUtc = null;
            existing.SeedVersion = "manual";
        }
        else
        {
            context.RolePermissions.Add(new RolePermission
            {
                Id = Guid.NewGuid(),
                RoleId = administrator.Id,
                PermissionId = resourceWrite.Id,
                IsActive = true,
                AssignedAtUtc = DateTime.UtcNow,
                SeedVersion = "manual"
            });
        }

        await context.SaveChangesAsync();

        var seeder = new SecurityBaselineSeeder(context, NullLogger<SecurityBaselineSeeder>.Instance);
        await seeder.SeedAsync();

        context.ChangeTracker.Clear();

        var reconciled = await context.RolePermissions.SingleAsync(rolePermission =>
            rolePermission.RoleId == administrator.Id && rolePermission.PermissionId == resourceWrite.Id);

        Assert.False(reconciled.IsActive);
        Assert.NotNull(reconciled.RevokedAtUtc);

        await seeder.SeedAsync();

        context.ChangeTracker.Clear();

        var afterSecondSeed = await context.RolePermissions.SingleAsync(rolePermission =>
            rolePermission.RoleId == administrator.Id && rolePermission.PermissionId == resourceWrite.Id);

        Assert.False(afterSecondSeed.IsActive);
    }

    /// <summary>
    /// `REQ-IDENTIDAD-30`: un permiso retirado del catálogo deja de estar activo, junto con sus asignaciones,
    /// para que ninguna política lo siga aceptando en bases que ya existían.
    /// </summary>
    [Fact]
    public async Task El_seed_revoca_un_permiso_que_ya_no_esta_en_el_catalogo()
    {
        await using var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);

        var administrator = await context.Roles.SingleAsync(role => role.Code == "ADMINISTRADOR");

        var legacyPermission = new Permission
        {
            Id = Guid.NewGuid(),
            PermissionKey = "LEGACY_READ",
            Description = "Permiso retirado del catálogo",
            IsActive = true,
            SeedVersion = "manual",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var legacyAssignment = new RolePermission
        {
            Id = Guid.NewGuid(),
            RoleId = administrator.Id,
            PermissionId = legacyPermission.Id,
            IsActive = true,
            AssignedAtUtc = DateTime.UtcNow,
            SeedVersion = "manual"
        };

        context.Permissions.Add(legacyPermission);
        context.RolePermissions.Add(legacyAssignment);

        await context.SaveChangesAsync();

        var seeder = new SecurityBaselineSeeder(context, NullLogger<SecurityBaselineSeeder>.Instance);
        await seeder.SeedAsync();

        context.ChangeTracker.Clear();

        var storedPermission = await context.Permissions
            .SingleAsync(permission => permission.PermissionKey == "LEGACY_READ");
        var storedAssignment = await context.RolePermissions
            .SingleAsync(rolePermission => rolePermission.Id == legacyAssignment.Id);

        Assert.False(storedPermission.IsActive);
        Assert.False(storedAssignment.IsActive);

        context.RolePermissions.Remove(storedAssignment);
        context.Permissions.Remove(storedPermission);

        await context.SaveChangesAsync();
    }

    private static async Task<(int Roles, int Permissions, int RolePermissions)> CountAsync(
        BudgetCivilDbContext context) =>
        (
            await context.Roles.CountAsync(),
            await context.Permissions.CountAsync(),
            await context.RolePermissions.CountAsync());
}
