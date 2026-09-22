using System.Net;
using System.Text.Json;
using BudgetCivil.Backend.BlackBoxTests.Infrastructure;
using BudgetCivil.Domain.Users.Entities;
using BudgetCivil.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetCivil.Backend.BlackBoxTests.Features.Security;

public sealed class FailClosedAuthorizationTests
{
    [Fact]
    public async Task GetUsers_Returns_Forbidden_When_Actor_Role_Is_Inactive_And_Audits_Denial()
    {
        using var factory = new TestWebApplicationFactory();

        var companyId = await SeedCompanyAsync(factory, "Fail Closed Role");
        var actor = await TestDataSeeder.SeedUserAsync(factory, "GERENTE_PROYECTOS", "Password1!", companyId: companyId);

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();
            var role = await dbContext.Roles.SingleAsync(existingRole => existingRole.Code == "GERENTE_PROYECTOS");
            role.IsActive = false;
            role.UpdatedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
        }

        var client = new AuthenticatedClientBuilder(factory)
            .WithUser(actor.UserId, "GERENTE_PROYECTOS", companyId)
            .Build();

        var response = await client.GetAsync("/api/v1/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var auditScope = factory.Services.CreateScope();
        var auditDbContext = auditScope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();

        var auditEvent = await auditDbContext.SecurityAuditEvents
            .Where(entry =>
                entry.ActorUserId == actor.UserId &&
                entry.PermissionKey == "USERS_READ" &&
                entry.ReasonCode == "ROLE_INACTIVE")
            .OrderByDescending(entry => entry.OccurredAtUtc)
            .FirstOrDefaultAsync();

        auditEvent.Should().NotBeNull();
        auditEvent!.Result.Should().Be(SecurityAuditResult.Denied);

        var metadata = JsonDocument.Parse(auditEvent.MetadataJson!).RootElement;
        metadata.GetProperty("actor_role_code").GetString().Should().Be("GERENTE_PROYECTOS");
        metadata.GetProperty("role_is_active").GetString().Should().Be("False");
        metadata.GetProperty("has_required_permission").GetString().Should().Be("True");
    }

    [Fact]
    public async Task GetUsers_Returns_Forbidden_When_RolePermission_Is_Inactive_And_Audits_Denial()
    {
        using var factory = new TestWebApplicationFactory();

        var companyId = await SeedCompanyAsync(factory, "Fail Closed Permission");
        var actor = await TestDataSeeder.SeedUserAsync(factory, "GERENTE_PROYECTOS", "Password1!", companyId: companyId);

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();

            var roleId = await dbContext.Roles
                .Where(role => role.Code == "GERENTE_PROYECTOS")
                .Select(role => role.Id)
                .SingleAsync();

            var permissionId = await dbContext.Permissions
                .Where(permission => permission.PermissionKey == "USERS_READ")
                .Select(permission => permission.Id)
                .SingleAsync();

            var rolePermission = await dbContext.RolePermissions
                .SingleAsync(existing => existing.RoleId == roleId && existing.PermissionId == permissionId);

            rolePermission.IsActive = false;
            rolePermission.RevokedAtUtc = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();
        }

        var client = new AuthenticatedClientBuilder(factory)
            .WithUser(actor.UserId, "GERENTE_PROYECTOS", companyId)
            .Build();

        var response = await client.GetAsync("/api/v1/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var auditScope = factory.Services.CreateScope();
        var auditDbContext = auditScope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();

        var auditEvent = await auditDbContext.SecurityAuditEvents
            .Where(entry =>
                entry.ActorUserId == actor.UserId &&
                entry.PermissionKey == "USERS_READ" &&
                entry.ReasonCode == "PERMISSION_INACTIVE_OR_MISSING")
            .OrderByDescending(entry => entry.OccurredAtUtc)
            .FirstOrDefaultAsync();

        auditEvent.Should().NotBeNull();
        auditEvent!.Result.Should().Be(SecurityAuditResult.Denied);

        var metadata = JsonDocument.Parse(auditEvent.MetadataJson!).RootElement;
        metadata.GetProperty("actor_role_code").GetString().Should().Be("GERENTE_PROYECTOS");
        metadata.GetProperty("role_is_active").GetString().Should().Be("True");
        metadata.GetProperty("has_required_permission").GetString().Should().Be("False");
    }

    private static async Task<Guid> SeedCompanyAsync(TestWebApplicationFactory factory, string prefix)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();

        var companyId = Guid.NewGuid();
        var token = companyId.ToString("N")[..10].ToUpperInvariant();

        dbContext.Companies.Add(new Company
        {
            Id = companyId,
            BusinessName = $"{prefix} {token}",
            TaxId = $"FC{token}"[..12],
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
        return companyId;
    }
}
