using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BudgetCivil.Backend.BlackBoxTests.Infrastructure;
using BudgetCivil.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetCivil.Backend.BlackBoxTests.Features.Users;

public sealed class AdminCreateUserAuthorizationTests
{
    [Fact]
    public async Task CreateUser_Allows_Administrador_And_Creates_Account()
    {
        using var factory = new TestWebApplicationFactory();

        var adminUser = await TestDataSeeder.SeedUserAsync(factory, "ADMINISTRADOR", "Password1!");

        var client = new AuthenticatedClientBuilder(factory)
            .WithUser(adminUser.UserId, "ADMINISTRADOR")
            .Build();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var location = await TestDataSeeder.SeedLocationAsync(factory);
        var payload = BuildCreateUserPayload(unique, location);

        var response = await client.PostAsJsonAsync("/api/v1/users", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var responsePayload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var createdUserId = responsePayload.GetProperty("user_id").GetGuid();

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();

        var createdUser = await dbContext.UserAccounts
            .Include(user => user.Role)
            .Include(user => user.Profile)
            .SingleAsync(user => user.Id == createdUserId);

        createdUser.Role.Code.Should().Be("PRESUPUESTADOR");
        createdUser.AccountEnabled.Should().BeTrue();
        createdUser.Profile!.CountryId.Should().Be(location.CountryId);
        createdUser.Profile.DepartmentId.Should().Be(location.DepartmentId);
        createdUser.Profile.MunicipalityId.Should().Be(location.MunicipalityId);
    }

    [Theory]
    [InlineData("GERENTE_PROYECTOS")]
    [InlineData("PRESUPUESTADOR")]
    [InlineData("AUDITOR")]
    public async Task CreateUser_Is_Forbidden_For_Non_Administrador_Roles(string actorRoleCode)
    {
        using var factory = new TestWebApplicationFactory();

        var actorCompanyId = Guid.NewGuid();
        var actorUser = await TestDataSeeder.SeedUserAsync(
            factory,
            actorRoleCode,
            "Password1!",
            companyId: actorCompanyId);

        var client = new AuthenticatedClientBuilder(factory)
            .WithUser(actorUser.UserId, actorRoleCode, actorCompanyId)
            .Build();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var location = await TestDataSeeder.SeedLocationAsync(factory);
        var payload = BuildCreateUserPayload(unique, location);

        var response = await client.PostAsJsonAsync("/api/v1/users", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static Dictionary<string, object?> BuildCreateUserPayload(string unique, SeededLocation location)
    {
        return new Dictionary<string, object?>
        {
            ["primer_nombre"] = "Ana",
            ["segundo_nombre"] = "Maria",
            ["primer_apellido"] = "Ruiz",
            ["segundo_apellido"] = "Lopez",
            ["tipo_documento"] = "CC",
            ["numero_documento"] = $"CC-{unique}",
            ["fecha_nacimiento"] = "1994-01-10",
            ["residencia"] = new Dictionary<string, object?>
            {
                ["country_id"] = location.CountryId,
                ["department_id"] = location.DepartmentId,
                ["municipality_id"] = location.MunicipalityId
            },
            ["email"] = $"ana.{unique}@mail.local",
            ["username"] = $"ana_{unique}",
            ["password"] = "Password1!",
            ["rol_asignado"] = "PRESUPUESTADOR"
        };
    }
}
