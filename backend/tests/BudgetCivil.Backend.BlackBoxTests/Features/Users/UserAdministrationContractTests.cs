using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BudgetCivil.Backend.BlackBoxTests.Infrastructure;
using BudgetCivil.Domain.Users.Entities;
using BudgetCivil.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetCivil.Backend.BlackBoxTests.Features.Users;

public sealed class UserAdministrationContractTests
{
    [Fact]
    public async Task UpdateUser_Updates_Profile_And_Returns_Ok()
    {
        using var factory = new TestWebApplicationFactory();

        var adminUser = await TestDataSeeder.SeedUserAsync(factory, "ADMINISTRADOR", "Password1!");
        var companyId = await SeedCompanyAsync(factory, "Update Contract Company");
        var targetUser = await TestDataSeeder.SeedUserAsync(factory, "PRESUPUESTADOR", "Password1!", companyId: companyId);

        var client = new AuthenticatedClientBuilder(factory)
            .WithUser(adminUser.UserId, "ADMINISTRADOR")
            .Build();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var payload = new Dictionary<string, object?>
        {
            ["primer_nombre"] = "Laura",
            ["segundo_nombre"] = "Elena",
            ["primer_apellido"] = "Suarez",
            ["segundo_apellido"] = "Mora",
            ["tipo_documento"] = "CC",
            ["numero_documento"] = $"UP-{unique}",
            ["fecha_nacimiento"] = "1992-03-08",
            ["residencia"] = new Dictionary<string, object?>
            {
                ["municipio"] = "Medellin",
                ["departamento"] = "Antioquia",
                ["pais"] = "Colombia"
            },
            ["email"] = $"updated.{unique}@mail.local",
            ["username"] = $"updated_{unique}",
            ["tarjeta_profesional"] = "TP-0099"
        };

        var response = await client.PutAsJsonAsync($"/api/v1/users/{targetUser.UserId}", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responsePayload = await response.Content.ReadFromJsonAsync<JsonElement>();
        responsePayload.GetProperty("user_id").GetGuid().Should().Be(targetUser.UserId);
        responsePayload.GetProperty("fecha_edicion").GetDateTime().Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();

        var updatedUser = await dbContext.UserAccounts
            .Include(user => user.Profile)
            .SingleAsync(user => user.Id == targetUser.UserId);

        updatedUser.Email.Should().Be($"updated.{unique}@mail.local");
        updatedUser.Username.Should().Be($"updated_{unique}");
        updatedUser.Profile!.FirstName.Should().Be("Laura");
        updatedUser.Profile.LastName.Should().Be("Suarez");
        updatedUser.Profile.ResidenceCity.Should().Be("Medellin");
    }

    [Fact]
    public async Task DeactivateUser_Disables_Account_And_Blocks_Login()
    {
        using var factory = new TestWebApplicationFactory();

        var adminUser = await TestDataSeeder.SeedUserAsync(factory, "ADMINISTRADOR", "Password1!");
        var targetUser = await TestDataSeeder.SeedUserAsync(factory, "PRESUPUESTADOR", "Password1!");

        var adminClient = new AuthenticatedClientBuilder(factory)
            .WithUser(adminUser.UserId, "ADMINISTRADOR")
            .Build();

        var deactivateResponse = await adminClient.PatchAsJsonAsync(
            $"/api/v1/users/{targetUser.UserId}/deactivate",
            new Dictionary<string, object?>
            {
                ["motivo"] = "Baja temporal"
            });

        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginClient = factory.CreateClient();
        var loginResponse = await loginClient.PostAsJsonAsync("/api/v1/auth/login", new
        {
            identifier = targetUser.Username,
            password = targetUser.Password
        });

        loginResponse.StatusCode.Should().Be((HttpStatusCode)423);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();
        var disabledUser = await dbContext.UserAccounts.SingleAsync(user => user.Id == targetUser.UserId);
        disabledUser.AccountEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task ChangeRole_Allows_Manager_To_Assign_Non_Admin_Role_Within_Own_Company()
    {
        using var factory = new TestWebApplicationFactory();

        var companyId = await SeedCompanyAsync(factory, "Role Change Company");
        var managerUser = await TestDataSeeder.SeedUserAsync(factory, "GERENTE_PROYECTOS", "Password1!", companyId: companyId);
        var targetUser = await TestDataSeeder.SeedUserAsync(factory, "PRESUPUESTADOR", "Password1!", companyId: companyId);

        var managerClient = new AuthenticatedClientBuilder(factory)
            .WithUser(managerUser.UserId, "GERENTE_PROYECTOS", companyId)
            .Build();

        var response = await managerClient.PatchAsJsonAsync(
            $"/api/v1/users/{targetUser.UserId}/role",
            new Dictionary<string, object?>
            {
                ["rol_asignado"] = "AUDITOR"
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();
        var updatedUser = await dbContext.UserAccounts
            .Include(user => user.Role)
            .SingleAsync(user => user.Id == targetUser.UserId);

        updatedUser.Role.Code.Should().Be("AUDITOR");
    }

    [Fact]
    public async Task ChangeRole_Rejects_Manager_Elevation_To_Administrador()
    {
        using var factory = new TestWebApplicationFactory();

        var companyId = await SeedCompanyAsync(factory, "Role Elevation Company");
        var managerUser = await TestDataSeeder.SeedUserAsync(factory, "GERENTE_PROYECTOS", "Password1!", companyId: companyId);
        var targetUser = await TestDataSeeder.SeedUserAsync(factory, "PRESUPUESTADOR", "Password1!", companyId: companyId);

        var managerClient = new AuthenticatedClientBuilder(factory)
            .WithUser(managerUser.UserId, "GERENTE_PROYECTOS", companyId)
            .Build();

        var response = await managerClient.PatchAsJsonAsync(
            $"/api/v1/users/{targetUser.UserId}/role",
            new Dictionary<string, object?>
            {
                ["rol_asignado"] = "ADMINISTRADOR"
            });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ChangeCompany_Assigns_Company_And_Creates_Active_Membership()
    {
        using var factory = new TestWebApplicationFactory();

        var adminUser = await TestDataSeeder.SeedUserAsync(factory, "ADMINISTRADOR", "Password1!");
        var targetUser = await TestDataSeeder.SeedUserAsync(factory, "PRESUPUESTADOR", "Password1!", companyId: null);
        var destinationCompanyId = await SeedCompanyAsync(factory, "Company Assignment");

        var adminClient = new AuthenticatedClientBuilder(factory)
            .WithUser(adminUser.UserId, "ADMINISTRADOR")
            .Build();

        var response = await adminClient.PatchAsJsonAsync(
            $"/api/v1/users/{targetUser.UserId}/company",
            new Dictionary<string, object?>
            {
                ["company_id"] = destinationCompanyId
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();
        var updatedUser = await dbContext.UserAccounts.SingleAsync(user => user.Id == targetUser.UserId);

        updatedUser.CompanyId.Should().Be(destinationCompanyId);

        var activeMembership = await dbContext.UserCompanyMemberships.SingleAsync(membership =>
            membership.UserId == targetUser.UserId && membership.IsActive);

        activeMembership.CompanyId.Should().Be(destinationCompanyId);
        activeMembership.AssignedByUserId.Should().Be(adminUser.UserId);
    }

    [Fact]
    public async Task CreateUser_Rejects_Project_Assignment_Fields()
    {
        using var factory = new TestWebApplicationFactory();

        var adminUser = await TestDataSeeder.SeedUserAsync(factory, "ADMINISTRADOR", "Password1!");

        var client = new AuthenticatedClientBuilder(factory)
            .WithUser(adminUser.UserId, "ADMINISTRADOR")
            .Build();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var payload = new Dictionary<string, object?>
        {
            ["primer_nombre"] = "Ana",
            ["primer_apellido"] = "Ruiz",
            ["tipo_documento"] = "CC",
            ["numero_documento"] = $"PJ-{unique}",
            ["fecha_nacimiento"] = "1990-01-01",
            ["residencia"] = new Dictionary<string, object?>
            {
                ["municipio"] = "Bogota",
                ["departamento"] = "Cundinamarca",
                ["pais"] = "Colombia"
            },
            ["email"] = $"project.{unique}@mail.local",
            ["username"] = $"project_{unique}",
            ["password"] = "Password1!",
            ["project_id"] = Guid.NewGuid()
        };

        var response = await client.PostAsJsonAsync("/api/v1/users", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static async Task<Guid> SeedCompanyAsync(TestWebApplicationFactory factory, string namePrefix)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();

        var companyId = Guid.NewGuid();
        var token = companyId.ToString("N")[..10].ToUpperInvariant();

        dbContext.Companies.Add(new Company
        {
            Id = companyId,
            BusinessName = $"{namePrefix} {token}",
            TaxId = $"TAX{token}"[..13],
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
        return companyId;
    }
}
